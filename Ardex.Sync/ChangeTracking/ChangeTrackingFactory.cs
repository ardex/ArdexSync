using System;
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
            ISyncRepositoryWithChangeTracking<TEntity, IChangeHistory> repository,
            ISyncRepository<IChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            this.InstallCustomChangeTracking(
                repository,
                changeHistory,
                (entity, action) =>
                {
                    var ch = (IChangeHistory)new ChangeHistory();

                    // Resolve pk.
                    ch.ChangeHistoryID = changeHistory
                        .AsUnsafeEnumerable()
                        .Select(c => c.ChangeHistoryID)
                        .DefaultIfEmpty()
                        .Max() + 1;

                    ch.Action = action;
                    ch.ReplicaID = this.ReplicaID;
                    ch.Timestamp = ChangeTrackingUtil.ResolveNextTimestamp(changeHistory, this.ReplicaID);
                    ch.UniqueID = uniqueIdMapping.Get(entity);

                    return ch;
                },
                (entity, remoteChangeHistory) =>
                {
                    var ch = (IChangeHistory)new ChangeHistory();

                    // Resolve pk.
                    ch.ChangeHistoryID = changeHistory
                        .AsUnsafeEnumerable()
                        .Select(c => c.ChangeHistoryID)
                        .DefaultIfEmpty()
                        .Max() + 1;

                    ch.Action = remoteChangeHistory.Action;
                    ch.ReplicaID = remoteChangeHistory.ReplicaID;
                    ch.Timestamp = remoteChangeHistory.Timestamp;
                    ch.UniqueID = remoteChangeHistory.UniqueID;

                    return ch;
                });
        }

        /// <summary>
        /// Creates links necessary for change tracking to work with
        /// a change history repository which tracks multiple articles.
        /// One change history repository is used by multiple data repositories.
        /// </summary>
        public void InstallSharedChangeTracking<TEntity>(SyncID articleID,
            ISyncRepositoryWithChangeTracking<TEntity, ISharedChangeHistory> repository,
            ISyncRepository<ISharedChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            throw new NotImplementedException();
        }

        public void InstallCustomChangeTracking<TEntity, TChangeHistory>(
            ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory> repository,
            ISyncRepository<TChangeHistory> changeHistory,
            Func<TEntity, ChangeHistoryAction, TChangeHistory> localChangeHistoryFactory,
            Func<TEntity, TChangeHistory, TChangeHistory> remoteChangeHistoryFactory)
        {
            if (repository.ProcessRemoteChangeHistoryEntry != null)
            {
                throw new InvalidOperationException(
                    "Unable to install change history link: repository already provisioned for change tracking.");
            }

            repository.EntityInserted += e => changeHistory.DirectInsert(localChangeHistoryFactory(e, ChangeHistoryAction.Insert));
            repository.EntityInserted += e => changeHistory.DirectUpdate(localChangeHistoryFactory(e, ChangeHistoryAction.Update));
            repository.EntityDeleted += e => changeHistory.DirectDelete(localChangeHistoryFactory(e, ChangeHistoryAction.Delete));

            repository.ProcessRemoteChangeHistoryEntry = (entity, remoteChangeHistory) =>
            {
                changeHistory.ObtainExclusiveLock();

                try
                {
                    var ch = remoteChangeHistoryFactory(entity, remoteChangeHistory);

                    changeHistory.DirectInsert(ch);
                }
                finally
                {
                    changeHistory.ReleaseExclusiveLock();
                }
            };
        }
    }
}
