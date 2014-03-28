using System;

using Ardex.Collections.Generic;

namespace Ardex.Sync
{
    /// <summary>
    /// IRepository which supports locking (used in sync operations).
    /// </summary>
    public interface ISyncRepository<TKey, TEntity> : IDictionaryRepository<TKey, TEntity>
    {
        /// <summary>
        /// Lock used to protect read and write
        /// operations in this repository.
        /// </summary>
        ISyncLock SyncLock { get; }

        /// <summary>
        /// Raised after a tracked insert, update or delete.
        /// </summary>
        event Action<TEntity, SyncEntityChangeAction> TrackedChange;

        /// <summary>
        /// Raised after an untracked insert, update or delete.
        /// </summary>
        event Action<TEntity, SyncEntityChangeAction> UntrackedChange;

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        void UntrackedInsert(TEntity entity);

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        void UntrackedUpdate(TEntity entity);
        
        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        void UntrackedDelete(TEntity entity);
    }
}