using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ardex.Collections;

namespace Ardex.Sync
{
    /// <summary>
    /// ProxyRepository implementation which supports locking (used in sync operations).
    /// </summary>
    public class SyncRepository<TEntity> : ProxyRepository<TEntity>, ISyncRepository<TEntity>
    {
        /// <summary>
        /// Backing field for Lock.
        /// </summary>
        private readonly ReaderWriterLockSlim __lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public bool ISyncRepository<TEntity>.SuppressChangeTracking { get; set; }

        /// <summary>
        /// Gets the number of entities in the respository.
        /// </summary>
        public override int Count
        {
            get
            {
                var count = 0;

                __lock.EnterReadLock();

                try
                {
                    count = base.Count;
                }
                finally
                {
                    __lock.ExitReadLock();
                }

                return count;
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SyncRepository() { }

        /// <summary>
        /// Initialises a new instance wrapping the given repository.
        /// </summary>
        public SyncRepository(IRepository<TEntity> repository) : base(repository) { }

        /// <summary>
        /// Initialises a new instance pre-populated with the given entities.
        /// </summary>
        public SyncRepository(IEnumerable<TEntity> entities) : base(entities) { }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        public override void Insert(TEntity entity)
        {
            __lock.EnterWriteLock();

            try
            {
                base.Insert(entity);
            }
            finally
            {
                __lock.ExitWriteLock();             
            }
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        public override void Update(TEntity entity)
        {
            __lock.EnterWriteLock();

            try
            {
                base.Update(entity);
            }
            finally
            {
                __lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        public override void Delete(TEntity entity)
        {
            __lock.EnterWriteLock();

            try
            {
                base.Delete(entity);
            }
            finally
            {
                __lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the enumerator over a snapshot
        /// of items as at this point in time.
        /// The collection is not locked while the
        /// snapshot is being iterated over, so it
        /// is possible for them to get out of sync
        /// if the underlying collection is modified
        /// before the enumerator is disposed of.
        /// </summary>
        public override IEnumerator<TEntity> GetEnumerator()
        {
            // We'll create a clone.
            __lock.EnterReadLock();

            var snapshot = default(List<TEntity>);

            try
            {
                snapshot = this.AsSyncRepository()
                               .AsUnsafeEnumerable()
                               .ToList();
            }
            finally
            {
                __lock.ExitReadLock();
            }

            return snapshot.GetEnumerator();
        }

        #region Explicit ISyncRepository implementation

        /// <summary>
        /// Performs a cast of this instance to a generic
        /// ISyncRepository and returns the result.
        /// </summary>
        private ISyncRepository<TEntity> AsSyncRepository()
        {
            return this;
        }

        /// <summary>
        /// Locks the repository so that no Insert,
        /// Update, Delete operations can be performed.
        /// </summary>
        void ISyncRepository<TEntity>.ObtainExclusiveLock()
        {
            __lock.EnterWriteLock();
        }

        /// <summary>
        /// Releases the exclusive lock held on this repository.
        /// </summary>
        void ISyncRepository<TEntity>.ReleaseExclusiveLock()
        {
            __lock.ExitWriteLock();
        }

        /// <summary>
        /// Creates an IEnumerable directly over the
        /// underlying collection without taking any locks.
        /// </summary>
        IEnumerable<TEntity> ISyncRepository<TEntity>.AsUnsafeEnumerable()
        {
            using (var enumerator = base.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        /// <summary>
        /// Performs the insert without any locking or change tracking.
        /// </summary>
        void ISyncRepository<TEntity>.DirectInsert(TEntity entity)
        {
            base.Insert(entity);
        }

        /// <summary>
        /// Performs the update without any locking or change tracking.
        /// </summary>
        void ISyncRepository<TEntity>.DirectUpdate(TEntity entity)
        {
            base.Update(entity);
        }

        /// <summary>
        /// Performs the delete without any locking or change tracking.
        /// </summary>
        void ISyncRepository<TEntity>.DirectDelete(TEntity entity)
        {
            base.Delete(entity);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Releases all resources used by this SynchronisedRepository.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                __lock.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}

