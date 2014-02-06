﻿using System;
using System.Linq;

using Ardex.Sync.Providers.ChangeBased;
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
        public void InstallExclusiveChangeTracking<TEntity>(
            ISyncRepository<TEntity> repository,
            ISyncRepository<IChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            this.InstallCustomChangeTracking(
                repository,
                (entity, action) =>
                {
                    changeHistory.ObtainExclusiveLock();

                    try
                    {
                        var ch = (IChangeHistory)new ChangeHistory();

                        // Resolve pk.
                        ch.ChangeHistoryID = changeHistory.Unlocked
                            .Select(c => c.ChangeHistoryID)
                            .DefaultIfEmpty()
                            .Max() + 1;


                        ch.Action = action;
                        ch.ReplicaID = this.ReplicaID;
                        ch.UniqueID = uniqueIdMapping.Get(entity);

                        // Resolve timestamp.
                        var timestamp = changeHistory.Unlocked
                            .Where(c => c.ReplicaID == this.ReplicaID)
                            .Select(c => c.Timestamp)
                            .DefaultIfEmpty()
                            .Max();

                        ch.Timestamp = (timestamp == null ? new Timestamp(1) : ++timestamp);

                        changeHistory.Unlocked.Insert(ch);
                    }
                    finally
                    {
                        changeHistory.ReleaseExclusiveLock();
                    }
                });
        }

        /// <summary>
        /// Creates links necessary for change tracking to work with
        /// a change history repository which tracks multiple articles.
        /// One change history repository is used by multiple data repositories.
        /// </summary>
        public void InstallSharedChangeTracking<TEntity>(SyncID articleID,
            ISyncRepository<TEntity> repository,
            ISyncRepository<ISharedChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            throw new NotImplementedException();
        }

        public void InstallCustomChangeTracking<TEntity>(
            ISyncRepository<TEntity> repository,
            Action<TEntity, ChangeHistoryAction> localChangeHandler)
        {
            //repository.EntityInserted += e =>
            //{
            //    if (!repository.SuppressChangeTracking)
            //        localChangeHandler(e, ChangeHistoryAction.Insert);
            //};

            //repository.EntityUpdated += e =>
            //{
            //    if (!repository.SuppressChangeTracking)
            //        localChangeHandler(e, ChangeHistoryAction.Update);
            //};

            //repository.EntityDeleted += e =>
            //{
            //    if (!repository.SuppressChangeTracking)
            //        localChangeHandler(e, ChangeHistoryAction.Delete);
            //};
        }
    }
}
