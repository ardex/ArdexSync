using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Ardex.Collections;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;
using Ardex.Sync.Providers.ChangeBased;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Sync provider implementation which works with
    /// sync repositories and change history metadata.
    /// </summary>
    public class ChangeTrackingSyncProvider<TEntity, TChangeHistory> :
        ISyncProvider<Dictionary<SyncID, Timestamp>, Change<TChangeHistory, TEntity>>,
        ISyncMetadataCleanup<Change<TChangeHistory, TEntity>>
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

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public ChangeTrackingSyncProvider(ChangeTracking<TEntity, TChangeHistory> changeTracking)
        {
            this.ChangeTracking = changeTracking;
        }

        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        public SyncResult AcceptChanges(SyncID replicaID, IEnumerable<Change<TChangeHistory, TEntity>> delta, CancellationToken ct)
        {
            // TODO: conflicts.

            // Critical region protected with exclusive lock.
            var repository = this.ChangeTracking.Repository;
            var changeHistory = this.ChangeTracking.FilteredChangeHistory();

            repository.Lock.EnterWriteLock();

            // Disable change tracking (which relies on events).
            this.ChangeTracking.Enabled = false;

            try
            {
                var type = typeof(TEntity);
                var inserts = new List<object>();
                var updates = new List<object>();
                var deletes = new List<object>();
                var props = type.GetProperties();

                // We need to ensure that all changes are processed in such
                // an order that if we fail, we'll be able to resume later.
                foreach (var change in delta.OrderBy(c => this.ChangeTracking.GetChangeHistoryTimestamp(c.ChangeHistory)))
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
                        repository.Insert(change.Entity);
                        inserts.Add(change);
                    }

                    // Write remote change history entry to local change history.
                    this.ChangeTracking.InsertChangeHistory(change.ChangeHistory);
                }

                ct.ThrowIfCancellationRequested();

                Debug.Print("{0} {1} inserts applied: {2}.", this.ChangeTracking.ReplicaID, type.Name, inserts.Count);
                Debug.Print("{0} {1} updates applied: {2}.", this.ChangeTracking.ReplicaID, type.Name, updates.Count);
                Debug.Print("{0} {1} deletes applied: {2}.", this.ChangeTracking.ReplicaID, type.Name, deletes.Count);

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
        public IEnumerable<Change<TChangeHistory, TEntity>> ResolveDelta(Dictionary<SyncID, Timestamp> timestampsByReplica, CancellationToken ct)
        {
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
                    (ch, entity) => new Change<TChangeHistory, TEntity>(ch, entity))
                // Ensure that the oldest changes for each replica are sync first.
                .OrderBy(c => this.ChangeTracking.GetChangeHistoryTimestamp(c.ChangeHistory))
                .AsEnumerable();

            return changes;
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
        public void CleanUpSyncMetadata(IEnumerable<Change<TChangeHistory, TEntity>> appliedDelta)
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

                foreach (var ch in changeHistory)
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
