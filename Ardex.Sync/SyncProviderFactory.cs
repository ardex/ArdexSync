using System.Collections.Generic;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;
using Ardex.Sync.Providers;

namespace Ardex.Sync
{
    public static class SyncProvider
    {
        public static ChangeHistorySyncProvider<TEntity> Create<TEntity>(
            SyncID replicaID, UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            var repository = new SyncRepository<TEntity>();

            return SyncProvider.Create(replicaID, repository, uniqueIdMapping);
        }

        public static ChangeHistorySyncProvider<TEntity> Create<TEntity>(
            SyncID replicaID, IEnumerable<TEntity> entities, UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            var repository = new SyncRepository<TEntity>(entities);

            return SyncProvider.Create(replicaID, repository, uniqueIdMapping);
        }

        public static ChangeHistorySyncProvider<TEntity> Create<TEntity>(
            SyncID replicaID, SyncRepository<TEntity> repository, UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            var changeHistory = new SyncRepository<IChangeHistory>();
            var provider = new ChangeHistorySyncProvider<TEntity>(replicaID, repository, new SyncRepository<IChangeHistory>(), uniqueIdMapping);

            // It is up to us to pre-populate change history.
            // Pretend that all those entities have just been inserted.
            foreach (var entity in repository)
            {
                provider.HandleRepositoryChange(entity, ChangeHistoryAction.Insert);
            }

            return provider;
        }

        public static ChangeHistorySyncProvider<TEntity> Create<TEntity>(
            SyncID replicaID, SyncRepository<TEntity> repository, SyncRepository<IChangeHistory> changeHistory, UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            return new ChangeHistorySyncProvider<TEntity>(replicaID, repository, changeHistory, uniqueIdMapping);
        }
    }
}
