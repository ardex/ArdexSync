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
        /// Locks the repository so that no Insert,
        /// Update, Delete operations can be performed.
        /// </summary>
        void ObtainExclusiveLock();

        /// <summary>
        /// Releases the exclusive lock held on this repository.
        /// </summary>
        void ReleaseExclusiveLock();

        /// <summary>
        /// Creates an IEnumerable directly over the
        /// underlying collection without taking any locks.
        /// </summary>
        IEnumerable<TEntity> AsUnsafeEnumerable();

        /// <summary>
        /// Performs the insert without any locking or change tracking.
        /// </summary>
        void DirectInsert(TEntity entity);

        /// <summary>
        /// Performs the update without any locking or change tracking.
        /// </summary>
        void DirectUpdate(TEntity entity);

        /// <summary>
        /// Performs the delete without any locking or change tracking.
        /// </summary>
        void DirectDelete(TEntity entity);
    }
}
