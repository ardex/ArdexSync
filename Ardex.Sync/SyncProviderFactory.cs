using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;
using Ardex.Sync.Providers;

namespace Ardex.Sync
{
    public static class SyncProvider
    {
        public static ChangeHistorySyncProvider<TEntity> Create<TEntity>(
            SyncID replicaID, SyncRepository<TEntity> repository, UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            return new ChangeHistorySyncProvider<TEntity>(replicaID, repository, new SyncRepository<IChangeHistory>(), uniqueIdMapping);
        }

        public static ChangeHistorySyncProvider<TEntity> Create<TEntity>(
            SyncID replicaID, SyncRepository<TEntity> repository, SyncRepository<IChangeHistory> changeHistory, UniqueIdMapping<TEntity> uniqueIdMapping)
        {
            return new ChangeHistorySyncProvider<TEntity>(replicaID, repository, changeHistory, uniqueIdMapping);
        }
    }
}
