using System.Collections.Generic;
using Ardex.Collections;

namespace Ardex.Sync
{
    /// <summary>
    /// IRepository with exclusive locking support.
    /// </summary>
    public interface ISyncRepository<TEntity> : IRepository<TEntity>
    {
        /// <summary>
        /// Provides access to the underlying
        /// repository without any locking.
        /// </summary>
        IRepository<TEntity> Unlocked { get; }

        /// <summary>
        /// Locks the repository so that no Insert,
        /// Update, Delete operations can be performed.
        /// </summary>
        void ObtainExclusiveLock();

        /// <summary>
        /// Releases the exclusive lock held on this repository.
        /// </summary>
        void ReleaseExclusiveLock();
    }
}
