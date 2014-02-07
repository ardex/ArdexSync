using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.Providers.ChangeBased;

namespace Ardex.Sync.Providers
{
    public enum SyncConflictResolutionStrategy
    {
        Fail,
        Winner,
        Loser
    }

    public static class MergeSyncProvider
    {
        /// <summary>
        /// Factory method.
        /// </summary>
        public static MergeSyncProvider<TEntity, TChangeHistory> Create<TEntity, TChangeHistory>(
            ChangeTracking<TEntity, TChangeHistory> changeTracking)
        {
            return new MergeSyncProvider<TEntity, TChangeHistory>(changeTracking);
        }
    }

    /// <summary>
    /// Sync provider implementation which works with
    /// sync repositories and change history metadata.
    /// </summary>
    public class MergeSyncProvider<TEntity, TChangeHistory> :
        ISyncProvider<Dictionary<SyncID, Timestamp>, Change<TEntity, TChangeHistory>>,
        ISyncMetadataCleanup<Change<TEntity, TChangeHistory>>
    {
        /// <summary>
        /// Gets the change tracking manager used by this provider.
        /// </summary>
        public ChangeTracking<TEntity, TChangeHistory> ChangeTracking { get; private set; }

        /// <summary>
        /// Gets the unique ID of the data replica
        /// that this provider works with.
        /// </summary>
        public SyncID ReplicaID
        {
            get { return this.ChangeTracking.ReplicaID; }
        }

        /// <summary>
        /// If true, the change history will be kept minimal
        /// by truncating all but the last entry at the end
        /// of the sync operation. Should only ever be enabled
        /// on the client in a client-server sync topology.
        /// The default is false.
        /// </summary>
        public bool CleanUpMetadataAfterSync { get; set; }

        public SyncConflictResolutionStrategy ConflictResolutionStrategy { get; set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public MergeSyncProvider(ChangeTracking<TEntity, TChangeHistory> changeTracking)
        {
            this.ChangeTracking = changeTracking;
        }

        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        public SyncResult AcceptChanges(
            SyncID sourceReplicaID, Delta<Dictionary<SyncID, Timestamp>, Change<TEntity, TChangeHistory>> delta, CancellationToken ct)
        {
            // Critical region protected with exclusive lock.
            var repository = this.ChangeTracking.Repository;

            repository.Lock.EnterWriteLock();

            // Disable change tracking (which relies on events).
            this.ChangeTracking.Enabled = false;

            try
            {
                // Materialise changes.
                var changes = delta.Changes.ToArray().AsEnumerable();

                // Detect conflicts.
                var myDelta = this.ResolveDelta(delta.Anchor, ct);

                var conflicts = myDelta.Changes.Join(
                    changes,
                    c => this.ChangeTracking.GetTrackedEntityID(c.Entity),
                    c => this.ChangeTracking.GetTrackedEntityID(c.Entity),
                    (local, remote) => SyncConflict.Create(local, remote));

                if (this.ConflictResolutionStrategy == SyncConflictResolutionStrategy.Fail)
                {
                    if (conflicts.Any())
                    {
                        throw new InvalidOperationException("Merge conflict detected.");
                    }
                }
                else if (this.ConflictResolutionStrategy == SyncConflictResolutionStrategy.Winner)
                {
                    var ignoredChanges = conflicts.Select(c => c.Remote);

                    foreach (var ignoredChange in ignoredChanges)
                    {
                        this.ChangeTracking.InsertChangeHistory(ignoredChange.ChangeHistory);
                    }

                    // Discard other replica's changes. Ours are better.
                    changes = changes.Except(ignoredChanges);
                }
                else if (this.ConflictResolutionStrategy == SyncConflictResolutionStrategy.Loser)
                {
                    // We'll just pretend that nothing happened.
                }

                var type = typeof(TEntity);
                var inserts = new List<object>();
                var updates = new List<object>();
                var deletes = new List<object>();
                var props = type.GetProperties();

                // We need to ensure that all changes are processed in such
                // an order that if we fail, we'll be able to resume later.
                foreach (var change in changes.OrderBy(c => this.ChangeTracking.GetChangeHistoryTimestamp(c.ChangeHistory)))
                {
                    ct.ThrowIfCancellationRequested();

                    var changeUniqueID = this.ChangeTracking.GetTrackedEntityID(change.Entity);
                    var found = false;

                    foreach (var existingEntity in repository)
                    {
                        if (changeUniqueID == this.ChangeTracking.GetTrackedEntityID(existingEntity))
                        {
                            // Found.
                            var changeCount = 0;

                            foreach (var prop in props)
                            {
                                if (prop.CanRead && prop.CanWrite)
                                {
                                    var oldValue = prop.GetValue(existingEntity);
                                    var newValue = prop.GetValue(change.Entity);

                                    if (!object.Equals(oldValue, newValue))
                                    {
                                        prop.SetValue(existingEntity, newValue);
                                        changeCount++;
                                    }
                                }
                            }

                            if (changeCount != 0)
                            {
                                repository.Update(existingEntity);
                                updates.Add(existingEntity);
                            }

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Debug only.
                        if (repository.Any(e => this.ChangeTracking.GetTrackedEntityID(e) == this.ChangeTracking.GetTrackedEntityID(change.Entity)))
                        {
                            throw new InvalidOperationException("PK violation detected.");
                        }

                        repository.Insert(change.Entity);
                        inserts.Add(change);
                    }

                    // Write remote change history entry to local change history.
                    this.ChangeTracking.InsertChangeHistory(change.ChangeHistory);
                }

                ct.ThrowIfCancellationRequested();

                Debug.Print("{0} applied {1} {2} inserts originating at {3}.", this.ReplicaID, inserts.Count, type.Name, sourceReplicaID);
                Debug.Print("{0} applied {1} {2} updates originating at {3}.", this.ReplicaID, updates.Count, type.Name, sourceReplicaID);
                Debug.Print("{0} applied {1} {2} deletes originating at {3}.", this.ReplicaID, deletes.Count, type.Name, sourceReplicaID);

                var result = new SyncResult(inserts, updates, deletes);

                return result;
            }
            finally
            {
                this.ChangeTracking.Enabled = true;

                repository.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Reports changes since the last reported timestamp for each node.
        /// </summary>
        public Delta<Dictionary<SyncID, Timestamp>, Change<TEntity, TChangeHistory>> ResolveDelta(Dictionary<SyncID, Timestamp> timestampsByReplica, CancellationToken ct)
        {
            this.ChangeTracking.ChangeHistory.Lock.EnterReadLock();

            try
            {
                var anchor = this.LastAnchor();

                var changes = this.ChangeTracking
                    .FilteredChangeHistory()
                    .Where(ch =>
                    {
                        var timestamp = default(Timestamp);

                        return
                            !timestampsByReplica.TryGetValue(this.ChangeTracking.GetChangeHistoryReplicaID(ch), out timestamp) ||
                            this.ChangeTracking.GetChangeHistoryTimestamp(ch) > timestamp;
                    })
                    .Join(
                        this.ChangeTracking.Repository.AsEnumerable(),
                        ch => this.ChangeTracking.GetChangeHistoryEntityID(ch),
                        this.ChangeTracking.GetTrackedEntityID,
                        (ch, entity) => Change.Create(entity, ch))
                    // Ensure that the oldest changes for each replica are sync first.
                    .OrderBy(c => this.ChangeTracking.GetChangeHistoryTimestamp(c.ChangeHistory))
                    .AsEnumerable();

                return new Delta<Dictionary<SyncID, Timestamp>, Change<TEntity, TChangeHistory>>(anchor, changes);
            }
            finally
            {
                this.ChangeTracking.ChangeHistory.Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns last seen timestamp value for each known node.
        /// </summary>
        public Dictionary<SyncID, Timestamp> LastAnchor()
        {
            return this.LastSeenTimestampByReplica(this.ChangeTracking.FilteredChangeHistory());
        }

        /// <summary>
        /// Returns last seen timestamp value for each known node.
        /// </summary>
        private Dictionary<SyncID, Timestamp> LastSeenTimestampByReplica(IEnumerable<TChangeHistory> changeHistory)
        {
            var dict = new Dictionary<SyncID, Timestamp>();

            foreach (var ch in changeHistory)
            {
                var replicaID = this.ChangeTracking.GetChangeHistoryReplicaID(ch);
                var timetsamp = this.ChangeTracking.GetChangeHistoryTimestamp(ch);
                var lastSeenTimestamp = default(Timestamp);

                if (!dict.TryGetValue(replicaID, out lastSeenTimestamp) || timetsamp > lastSeenTimestamp)
                {
                    dict[replicaID] = timetsamp;
                }
            }

            return dict;
        }

        /// <summary>
        /// Performs change history cleanup if necessary.
        /// Ensures that only the latest value for each node is kept.
        /// </summary>
        public void CleanUpSyncMetadata(IEnumerable<Change<TEntity, TChangeHistory>> appliedDelta)
        {
            if (!this.CleanUpMetadataAfterSync)
                return;

            var changeHistory = this.ChangeTracking.ChangeHistory;

            // We need exclusive access to change
            // history during the cleanup operation.
            changeHistory.Lock.EnterWriteLock();

            try
            {
                var lastCommittedTimestampByReplica = this.LastSeenTimestampByReplica(appliedDelta.Select(c => c.ChangeHistory));

                foreach (var ch in this.ChangeTracking.FilteredChangeHistory())
                {
                    // Ensure that this change is not the last for node.
                    var replicaID = this.ChangeTracking.GetChangeHistoryReplicaID(ch);
                    var timestamp = this.ChangeTracking.GetChangeHistoryTimestamp(ch);
                    var lastCommittedTimestamp = default(Timestamp);

                    if (lastCommittedTimestampByReplica.TryGetValue(replicaID, out lastCommittedTimestamp) &&
                        timestamp < lastCommittedTimestamp)
                    {
                        changeHistory.Delete(ch);
                    }
                }
            }
            finally
            {
                changeHistory.Lock.ExitWriteLock();
            }
        }
    }
}
