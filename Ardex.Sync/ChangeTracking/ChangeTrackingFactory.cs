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
            ISyncRepository<TEntity> repository,
            ISyncRepository<IChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            this.InstallCustomChangeTracking(
                repository,
                (entity, action) =>
                {
                    // Todo: do properly.
                    var lastChangeEntry = changeHistory
                        .AsUnsafeEnumerable()
                        .Where(c => c.UniqueID == uniqueIdMapping.Get(entity))
                        .OrderBy(c => c.ChangeHistoryID)
                        .LastOrDefault();

                    if (lastChangeEntry != null && lastChangeEntry.ReplicaID != this.ReplicaID)
                    {
                        return;
                    }

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

                    changeHistory.DirectInsert(ch);
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
            Action<TEntity, ChangeHistoryAction> localChangeHistoryFactory)
        {
            repository.EntityInserted += e => localChangeHistoryFactory(e, ChangeHistoryAction.Insert);
            repository.EntityUpdated += e => localChangeHistoryFactory(e, ChangeHistoryAction.Update);
            repository.EntityDeleted += e => localChangeHistoryFactory(e, ChangeHistoryAction.Delete);
        }
    }
}
