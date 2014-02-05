using System;

namespace Ardex.Sync.ChangeTracking
{
    public interface ISyncRepositoryWithChangeTracking<TEntity> : ISyncRepository<TEntity>
    {
        // Change tracking provisions.
        Action<TEntity> TrackInsert { get; set; }
        Action<TEntity> TrackUpdate { get; set; }
        Action<TEntity> TrackDelete { get; set; }
    }
}
