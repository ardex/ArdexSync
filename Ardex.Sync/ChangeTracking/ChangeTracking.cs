using System;
using System.Collections.Generic;
using System.Linq;

using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.ChangeTracking
{
    public class ChangeTracking<TEntity, TChangeHistory>
    {
        private readonly SyncRepository<TChangeHistory> __changeHistory;

        /// <summary>
        /// Gets the unique ID of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Gets the tracked repository.
        /// </summary>
        public SyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Gets the change history repository which contains
        /// the change metadata for the tracked repository.
        /// </summary>
        public SyncRepository<TChangeHistory> ChangeHistory
        {
            get { return __changeHistory; }
        }

        private readonly Func<TChangeHistory, bool> ChangeHistoryPredicate;

        // Essential member mapping.
        public readonly UniqueIdMapping<TEntity> TrackedEntityIdMapping;
        private readonly UniqueIdMapping<TChangeHistory> ChangeHistoryIdMapping;
        private readonly UniqueIdMapping<TChangeHistory> ChangeHistoryEntityIdMapping;
        private readonly UniqueIdMapping<TChangeHistory> ChangeHistoryReplicaIdMapping;
        private readonly ComparableMapping<TChangeHistory> ChangeHistoryVersionMapping;

        // Tracked/untracked change actions.
        // Note that it's the repository's responsibility
        // to actually insert/update/delete entities.
        // All we need to do is create change history entries to reflect the change.
        private readonly Action<TEntity, ChangeHistoryAction> __trackedChange;
        private readonly Action<TChangeHistory> __insertChangeHistory;

        /// <summary>
        /// Indicates whether change tracking is turned on.
        /// </summary>
        public bool Enabled { get; internal set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public ChangeTracking(
            SyncID replicaID,
            SyncRepository<TEntity> entities,
            SyncRepository<TChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> trackedEntityIdMapping,
            Func<TChangeHistory, bool> changeHistoryPredicate,
            UniqueIdMapping<TChangeHistory> changeHistoryIdMapping,
            UniqueIdMapping<TChangeHistory> changeHistoryEntityIdMapping,
            UniqueIdMapping<TChangeHistory> changeHistoryReplicaIdMapping,
            ComparableMapping<TChangeHistory> changeHistoryVersionMapping,
            Action<TEntity, ChangeHistoryAction> trackedChange,
            Action<TChangeHistory> insertChangeHistory)
        {
            // Linked repositories.
            this.ReplicaID = replicaID;
            this.Repository = entities;

            __changeHistory = changeHistory;

            this.ChangeHistoryPredicate = changeHistoryPredicate;

            // Essential member mapping.
            this.TrackedEntityIdMapping = trackedEntityIdMapping;
            this.ChangeHistoryIdMapping = changeHistoryIdMapping;
            this.ChangeHistoryEntityIdMapping = changeHistoryEntityIdMapping;
            this.ChangeHistoryReplicaIdMapping = changeHistoryReplicaIdMapping;
            this.ChangeHistoryVersionMapping = changeHistoryVersionMapping;

            __trackedChange = trackedChange;
            __insertChangeHistory = insertChangeHistory;

            // Defaults.
            this.Enabled = true;

            // Events.
            this.Repository.EntityInserted += e => this.TrackedChange(e, ChangeHistoryAction.Insert);
            this.Repository.EntityUpdated += e => this.TrackedChange(e, ChangeHistoryAction.Update);
            this.Repository.EntityDeleted += e => this.TrackedChange(e, ChangeHistoryAction.Delete);
        }

        /// <summary>
        /// Returns the the change history collection filtered according
        /// to the predicate specified when this instance was created.
        /// </summary>
        public IEnumerable<TChangeHistory> FilteredChangeHistory()
        {
            return __changeHistory.Where(this.ChangeHistoryPredicate);
        }

        /// <summary>
        /// Creates change history entries following an entity
        /// insert/update/delete (provided that change tracking is enabled).
        /// </summary>
        private void TrackedChange(TEntity entity, ChangeHistoryAction action)
        {
            if (!this.Enabled)
                return;

            __changeHistory.Lock.EnterWriteLock();

            try
            {
                __trackedChange(entity, action);
            }
            finally
            {
                __changeHistory.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Creates local change history entry mirroring
        /// the change history entry which is returned by
        /// a remote replica during change-based sync.
        /// </summary>
        public void InsertChangeHistory(TChangeHistory changeHistoryEntry)
        {
            __changeHistory.Lock.EnterWriteLock();

            try
            {
                __insertChangeHistory(changeHistoryEntry);
            }
            finally
            {
                __changeHistory.Lock.ExitWriteLock();
            }
        }

        public SyncID GetTrackedEntityID(TEntity entity)
        {
            return this.TrackedEntityIdMapping.Get(entity);
        }

        public SyncID GetChangeHistoryID(TChangeHistory changeHistory)
        {
            return this.ChangeHistoryIdMapping.Get(changeHistory);
        }

        public SyncID GetChangeHistoryEntityID(TChangeHistory changeHistory)
        {
            return this.ChangeHistoryEntityIdMapping.Get(changeHistory);
        }

        public SyncID GetChangeHistoryReplicaID(TChangeHistory changeHistory)
        {
            return this.ChangeHistoryReplicaIdMapping.Get(changeHistory);
        }

        public IComparable GetChangeHistoryVersion(TChangeHistory changeHistory)
        {
            return this.ChangeHistoryVersionMapping.Get(changeHistory);
        }
    }
}
