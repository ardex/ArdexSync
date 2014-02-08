using System;
using System.Linq;

using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Facilitates change history installation.
    /// </summary>
    public class ChangeTrackingFactory
    {
        /// <summary>
        /// Unique ID of the replica which tracks the repository changes.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public ChangeTrackingFactory(SyncID replicaID)
        {
            this.ReplicaID = replicaID;
        }

        /// <summary>
        /// Creates links necessary for change tracking to work with
        /// a change history repository which tracks a single article.
        /// One change history repository is used exclusively by one data repository.
        /// </summary>
        public ChangeTrackingRegistration<TEntity, IChangeHistory> Exclusive<TEntity>(
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistoryRepository,
            UniqueIdMapping<TEntity> entityIdMapping)
        {
            var changeTracking = new ChangeTrackingRegistration<TEntity, IChangeHistory>(
                this.ReplicaID,
                repository,
                changeHistoryRepository,
                entityIdMapping,
                ch => true,
                new UniqueIdMapping<IChangeHistory>(ch => ch.ChangeHistoryID),
                new UniqueIdMapping<IChangeHistory>(ch => ch.UniqueID),
                new UniqueIdMapping<IChangeHistory>(ch => ch.ReplicaID),
                new ComparisonComparer<IChangeHistory>((x, y) => x.Timestamp.CompareTo(y.Timestamp)));

            changeTracking.TrackedChange += (entity, action) =>
            {
                var changeHistory = changeTracking.ChangeHistory;
                var ch = (IChangeHistory)new ChangeHistory();

                // Resolve pk.
                ch.ChangeHistoryID = changeHistory
                    .Select(c => c.ChangeHistoryID)
                    .DefaultIfEmpty()
                    .Max() + 1;

                ch.Action = action;
                ch.ReplicaID = this.ReplicaID;
                ch.UniqueID = changeTracking.GetTrackedEntityID(entity);

                // Resolve version.
                var timestamp = changeHistory
                    .Where(c => c.ReplicaID == this.ReplicaID)
                    .Select(c => c.Timestamp)
                    .DefaultIfEmpty()
                    .Max();

                ch.Timestamp = (timestamp == null ? new Timestamp(1) : ++timestamp);

                changeHistory.Insert(ch);
            };

            changeTracking.DirectInsertRequest += changeHistoryEntry =>
            {
                var changeHistory = changeTracking.ChangeHistory;
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
            };

            return changeTracking;
        }

        /// <summary>
        /// Creates links necessary for change tracking to work with
        /// a change history repository which tracks multiple articles.
        /// One change history repository is used by one or more repositories.
        /// </summary>
        public ChangeTrackingRegistration<TEntity, ISharedChangeHistory> Shared<TEntity>(
            SyncID articleID,
            SyncRepository<TEntity> repository,
            SyncRepository<ISharedChangeHistory> changeHistoryRepository,
            UniqueIdMapping<TEntity> entityIdMapping)
        {
            throw new NotImplementedException();

            //var changeTracking = default(ChangeTrackingRegistration<TEntity, ISharedChangeHistory>);

            //changeTracking = new ChangeTrackingRegistration<TEntity, ISharedChangeHistory>(
            //    this.ReplicaID,
            //    repository,
            //    changeHistoryRepository,
            //    entityIdMapping,
            //    ch => ch.ArticleID == articleID,
            //    new UniqueIdMapping<ISharedChangeHistory>(ch => ch.ChangeHistoryID),
            //    new UniqueIdMapping<ISharedChangeHistory>(ch => ch.UniqueID),
            //    new UniqueIdMapping<ISharedChangeHistory>(ch => ch.ReplicaID),
            //    new ComparableMapping<ISharedChangeHistory>(ch => ch.Timestamp),
            //    (entity, action) =>
            //    {
            //        var changeHistory = changeTracking.ChangeHistory;
            //        var ch = (ISharedChangeHistory)new SharedChangeHistory();

            //        // Resolve pk.
            //        ch.ChangeHistoryID = changeHistory
            //            .Select(c => c.ChangeHistoryID)
            //            .DefaultIfEmpty()
            //            .Max() + 1;

            //        ch.ArticleID = articleID;
            //        ch.Action = action;
            //        ch.ReplicaID = this.ReplicaID;
            //        ch.UniqueID = changeTracking.GetTrackedEntityID(entity);

            //        // Resolve version.
            //        var timestamp = changeHistory
            //            .Where(c => c.ReplicaID == this.ReplicaID)
            //            .Select(c => c.Timestamp)
            //            .DefaultIfEmpty()
            //            .Max();

            //        ch.Timestamp = (timestamp == null ? new Timestamp(1) : ++timestamp);

            //        changeHistory.Insert(ch);
            //    },
            //    changeHistoryEntry =>
            //    {
            //        var changeHistory = changeTracking.ChangeHistory;
            //        var ch = (ISharedChangeHistory)new SharedChangeHistory();

            //        // Resolve pk.
            //        ch.ChangeHistoryID = changeHistory
            //            .Select(c => c.ChangeHistoryID)
            //            .DefaultIfEmpty()
            //            .Max() + 1;

            //        ch.ArticleID = articleID;
            //        ch.Action = changeHistoryEntry.Action;
            //        ch.ReplicaID = changeHistoryEntry.ReplicaID;
            //        ch.UniqueID = changeHistoryEntry.UniqueID;
            //        ch.Timestamp = changeHistoryEntry.Timestamp;

            //        changeHistory.Insert(ch);
            //    });

            //return changeTracking;
        }
    }
}
