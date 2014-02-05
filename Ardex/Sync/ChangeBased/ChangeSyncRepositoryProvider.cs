using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.ChangeBased
{
    /// <summary>
    /// Sync provider implementation which works with
    /// sync repositories and change history metadata.
    /// </summary>
    public class ChangeSyncRepositoryProvider<TEntity> :
        ISyncProvider<Dictionary<SyncID, Timestamp>, Change<TEntity>>,
        ISyncMetadataCleanup<Change<TEntity>>
    {
        /// <summary>
        /// Gets this sync node's unique identifier.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Entity storage.
        /// </summary>
        public ISyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Change history storage.
        /// </summary>
        public ISyncRepository<IChangeHistory> ChangeHistory { get; private set; }

        /// <summary>
        /// Provides means of uniquely identifying an entity.
        /// </summary>
        public UniqueIdMapping<TEntity> UniqueIdMapping { get; private set; }

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
        public ChangeSyncRepositoryProvider(
            SyncID replicaID,
            ISyncRepository<TEntity> repository,
            ISyncRepository<IChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            this.ReplicaID = replicaID;
            this.Repository = repository;
            this.ChangeHistory = changeHistory;
            this.UniqueIdMapping = uniqueIdMapping;
        }

        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        public SyncResult AcceptChanges(SyncID replicaID, IEnumerable<Change<TEntity>> delta, CancellationToken ct)
        {
            // TODO: conflicts.

            // Critical region protected with exclusive lock.
            this.Repository.ObtainExclusiveLock();

            try
            {
                var type = typeof(TEntity);
                var inserts = new List<object>();
                var updates = new List<object>();
                var deletes = new List<object>();
                var props = type.GetProperties();

                // We need to ensure that all changes are processed in such
                // an order that if we fail, we'll be able to resume later.
                foreach (var change in delta.OrderBy(c => c.ChangeHistory.Timestamp))
                {
                    ct.ThrowIfCancellationRequested();

                    var changeUniqueID = this.UniqueIdMapping.Get(change.Entity);
                    var found = false;

                    // Again, we are accessing snapshot in
                    // order to avoid recursive locking.
                    foreach (var existingEntity in this.Repository.AsUnsafeEnumerable())
                    {
                        if (changeUniqueID == this.UniqueIdMapping.Get(existingEntity))
                        {
                            // Found.
                            var changeCount = 0;

                            foreach (var prop in props)
                            {
                                if (prop.CanRead && prop.CanWrite)
                                {
                                    var oldValue = prop.GetValue(existingEntity);
                                    var newValue = prop.GetValue(change.Entity);

                                    #if FORCE_FULL_SYNC
                                    const bool force = true;
                                    #else
                                    const bool force = false;
                                    #endif

                                    if (force || !object.Equals(oldValue, newValue))
                                    {
                                        prop.SetValue(existingEntity, newValue);
                                        changeCount++;
                                    }
                                }
                            }

                            if (changeCount != 0)
                            {
                                this.Repository.DirectUpdate(existingEntity);
                                updates.Add(existingEntity);
                            }

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        this.Repository.DirectInsert(change.Entity);
                        inserts.Add(change);
                    }

                    // Write remote change history entry.
                    ChangeHistoryUtil.WriteRemoteChangeHistory(
                        this.ChangeHistory,
                        change.Entity,
                        change.ChangeHistory.ReplicaID,
                        change.ChangeHistory.Timestamp,
                        this.UniqueIdMapping,
                        change.ChangeHistory.Action);
                }

                ct.ThrowIfCancellationRequested();

                Debug.Print("{0} {1} inserts applied: {2}.", this.ReplicaID, type.Name, inserts.Count);
                Debug.Print("{0} {1} updates applied: {2}.", this.ReplicaID, type.Name, updates.Count);
                Debug.Print("{0} {1} deletes applied: {2}.", this.ReplicaID, type.Name, deletes.Count);

                var result = new SyncResult(inserts, updates, deletes);

                return result;
            }
            finally
            {
                this.Repository.ReleaseExclusiveLock();
            }
        }

        /// <summary>
        /// Reports changes since the last reported timestamp for each node.
        /// </summary>
        public IEnumerable<Change<TEntity>> ResolveDelta(Dictionary<SyncID, Timestamp> timestampsByReplica, int batchSize, CancellationToken ct)
        {
            var changes = this.ChangeHistory
                .Where(ch =>
                {
                    var timestamp = default(Timestamp);

                    return (!timestampsByReplica.TryGetValue(ch.ReplicaID, out timestamp) || ch.Timestamp > timestamp);
                })
                .Join(
                    this.Repository.AsEnumerable(),
                    ch => ch.UniqueID,
                    this.UniqueIdMapping.Get,
                    (ch, entity) => new Change<TEntity>(ch, entity))
                // Ensure that the oldest changes for each replica are sync first.
                .OrderBy(c => c.ChangeHistory.Timestamp)
                .AsEnumerable();

            // Limit batch size if we have to.
            if (batchSize != 0)
            {
                changes = changes.Take(batchSize);
            }

            return changes;
        }

        /// <summary>
        /// Returns last seen timestamp value for each known node.
        /// </summary>
        public Dictionary<SyncID, Timestamp> LastAnchor()
        {
            return this.LastSeenTimestampByReplica(this.ChangeHistory.AsEnumerable());
        }

        /// <summary>
        /// Returns last seen timestamp value for each known node.
        /// </summary>
        private Dictionary<SyncID, Timestamp> LastSeenTimestampByReplica(IEnumerable<IChangeHistory> changeHistoryCol)
        {
            var dict = new Dictionary<SyncID, Timestamp>();

            foreach (var changeHistory in changeHistoryCol)
            {
                var lastSeenTimestamp = default(Timestamp);

                if (!dict.TryGetValue(changeHistory.ReplicaID, out lastSeenTimestamp) ||
                    changeHistory.Timestamp > lastSeenTimestamp)
                {
                    dict[changeHistory.ReplicaID] = changeHistory.Timestamp;
                }
            }

            return dict;
        }

        /// <summary>
        /// Performs change history cleanup if necessary.
        /// Ensures that only the latest value for each node is kept.
        /// </summary>
        public void CleanUpSyncMetadata(IEnumerable<Change<TEntity>> changes)
        {
            if (!this.CleanUpMetadataAfterSync)
                return;

            // We need exclusive access to change
            // history during the cleanup operation.
            this.ChangeHistory.ObtainExclusiveLock();

            try
            {
                var lastCommittedTimestampByReplica = this.LastSeenTimestampByReplica(changes.Select(c => c.ChangeHistory));
                var snapshot = this.ChangeHistory.AsUnsafeEnumerable().ToList();

                foreach (var changeHistory in snapshot)
                {
                    // Ensure that this change is not the last for node.
                    var lastCommittedTimestamp = default(Timestamp);

                    if (lastCommittedTimestampByReplica.TryGetValue(changeHistory.ReplicaID, out lastCommittedTimestamp) &&
                        changeHistory.Timestamp < lastCommittedTimestamp)
                    {
                        this.ChangeHistory.DirectDelete(changeHistory);
                    }
                }
            }
            finally
            {
                this.ChangeHistory.ReleaseExclusiveLock();
            }
        }
    }
}
