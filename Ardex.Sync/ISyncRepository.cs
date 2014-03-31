using System;

using Ardex.Collections.Generic;

namespace Ardex.Sync
{
    /// <summary>
    /// IRepository which supports locking (used in sync operations).
    /// </summary>
    public interface ISyncRepository<TKey, TEntity> : IKeyRepository<TKey, TEntity>
    {
        /// <summary>
        /// Lock used to protect read and write
        /// operations in this repository.
        /// </summary>
        ISyncLock SyncLock { get; }

        /// <summary>
        /// Raised after a tracked insert, update or delete.
        /// </summary>
        event SyncRepositoryChangeEventHandler<TEntity> Changed;

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        void Insert(TEntity entity, SyncRepositoryChangeMode changeMode);

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        void Update(TEntity entity, SyncRepositoryChangeMode changeMode);
        
        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        void Delete(TEntity entity, SyncRepositoryChangeMode changeMode);
    }
}