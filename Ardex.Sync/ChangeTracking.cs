using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync
{
    public class ChangeTracking<TEntity>
    {
        public SyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Gets or sets the value controlling
        /// whether this instance raises events
        /// in response to changes in the parent
        /// repository.
        /// </summary>
        public bool Enabled { get; set; }

        public event Action<TEntity, ChangeHistoryAction> TrackedChange;

        public ChangeTracking(SyncRepository<TEntity> repository)
        {
            this.Repository = repository;

            // Hook up events.
            repository.EntityInserted += this.OnTrackedInsert;
            repository.EntityUpdated += this.OnTrackedUpdate;
            repository.EntityDeleted += this.OnTrackedDelete;
        }

        protected virtual void OnTrackedInsert(TEntity entity)
        {
            if (this.Enabled && this.TrackedChange != null)
            {
                this.TrackedChange(entity, ChangeHistoryAction.Insert);
            }
        }

        protected virtual void OnTrackedUpdate(TEntity entity)
        {
            if (this.Enabled && this.TrackedChange != null)
            {
                this.TrackedChange(entity, ChangeHistoryAction.Update);
            }
        }

        protected virtual void OnTrackedDelete(TEntity entity)
        {
            if (this.Enabled && this.TrackedChange != null)
            {
                this.TrackedChange(entity, ChangeHistoryAction.Delete);
            }
        }

        private readonly Dictionary<Type, object> Registrations = new Dictionary<Type, object>();

        public ChangeTrackingRegistration<TEntity, TVersion> GetRegistration<TVersion>()
        {
            var type = typeof(TVersion);
            var obj = default(object);

            if (this.Registrations.TryGetValue(type, out obj))
            {
                return (ChangeTrackingRegistration<TEntity, TVersion>)obj;
            }

            return null;
        }

        public void SetUp(SyncRepository<IChangeHistory> changeHistory, SyncID replicaID, UniqueIdMapping<TEntity> entityIdMapping)
        {
            var changeTracking = default(ChangeTrackingRegistration<TEntity, IChangeHistory>);

            changeTracking = new ChangeTrackingRegistration<TEntity, IChangeHistory>(
                replicaID,
                this.Repository,
                changeHistory,
                entityIdMapping,
                ch => true,
                new UniqueIdMapping<IChangeHistory>(ch => ch.ChangeHistoryID),
                new UniqueIdMapping<IChangeHistory>(ch => ch.UniqueID),
                new UniqueIdMapping<IChangeHistory>(ch => ch.ReplicaID),
                new ComparableMapping<IChangeHistory>(ch => ch.Timestamp),
                (entity, action) =>
                {
                    var ch = (IChangeHistory)new ChangeHistory();

                    // Resolve pk.
                    ch.ChangeHistoryID = changeHistory
                        .Select(c => c.ChangeHistoryID)
                        .DefaultIfEmpty()
                        .Max() + 1;

                    ch.Action = action;
                    ch.ReplicaID = replicaID;
                    ch.UniqueID = changeTracking.GetTrackedEntityID(entity);

                    // Resolve version.
                    var timestamp = changeHistory
                        .Where(c => c.ReplicaID == replicaID)
                        .Select(c => c.Timestamp)
                        .DefaultIfEmpty()
                        .Max();

                    ch.Timestamp = (timestamp == null ? new Timestamp(1) : ++timestamp);

                    changeHistory.Insert(ch);
                },
                changeHistoryEntry =>
                {
                    var ch = (IChangeHistory)new ChangeHistory();

                    // Resolve pk.
                    ch.ChangeHistoryID = changeHistory
                        .Select(c => c.ChangeHistoryID)
                        .DefaultIfEmpty()
                        .Max() + 1;

                    ch.Action = changeHistoryEntry.Action;
                    ch.ReplicaID = changeHistoryEntry.ReplicaID;
                    ch.UniqueID = changeHistoryEntry.UniqueID;
                    ch.Timestamp = changeHistoryEntry.Timestamp;

                    changeHistory.Insert(ch);
                });

            this.Registrations.Add(typeof(IChangeHistory), changeTracking);
        }
    }
}
