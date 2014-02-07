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
    public class SyncRepository<TEntity> : ProxyRepository<TEntity>
    {
        /// <summary>
        /// Backing field for Lock.
        /// </summary>
        private readonly ReaderWriterLockSlim __lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public ReaderWriterLockSlim Lock
        {
            get { return __lock; }
        }

        /// <summary>
        /// Gets the number of entities in the respository.
        /// </summary>
        public override int Count
        {
            get
            {
                var count = 0;

                this.Lock.EnterReadLock();

                try
                {
                    count = base.Count;
                }
                finally
                {
                    this.Lock.ExitReadLock();
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
            this.Lock.EnterWriteLock();

            try
            {
                base.Insert(entity);
            }
            finally
            {
                this.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        public override void Update(TEntity entity)
        {
            this.Lock.EnterWriteLock();

            try
            {
                base.Update(entity);
            }
            finally
            {
                this.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        public override void Delete(TEntity entity)
        {
            this.Lock.EnterWriteLock();

            try
            {
                base.Delete(entity);
            }
            finally
            {
                this.Lock.ExitWriteLock();
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
            var snapshot = default(List<TEntity>);

            this.Lock.EnterReadLock();

            try
            {
                snapshot = this.InnerRepository.ToList();
            }
            finally
            {
                this.Lock.ExitReadLock();
            }

            return snapshot.GetEnumerator();
        }

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

