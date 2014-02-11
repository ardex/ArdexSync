using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Ardex.Collections;
using Ardex.Sync.ChangeTracking;

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

        /// <summary>
        /// Lock used to protect read and write
        /// operations in this repository.
        /// </summary>
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
                this.Lock.EnterReadLock();

                try
                {
                    return base.Count;
                }
                finally
                {
                    this.Lock.ExitReadLock();
                }
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
        /// Raised after a tracked insert, update or delete.
        /// </summary>
        public event Action<TEntity, ChangeHistoryAction> TrackedChange;

        /// <summary>
        /// Raised after an untracked insert, update or delete.
        /// </summary>
        public event Action<TEntity, ChangeHistoryAction> UntrackedChange;

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

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, ChangeHistoryAction.Insert);
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

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, ChangeHistoryAction.Update);
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

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, ChangeHistoryAction.Delete);
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
            this.Lock.EnterReadLock();

            try
            {
                // We'll create a clone.
                var snapshot = this.InnerRepository.ToList();

                return snapshot.GetEnumerator();
            }
            finally
            {
                this.Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        internal void UntrackedInsert(TEntity entity)
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

            if (this.UntrackedChange != null)
            {
                this.UntrackedChange(entity, ChangeHistoryAction.Insert);
            }
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        internal void UntrackedUpdate(TEntity entity)
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

            if (this.UntrackedChange != null)
            {
                this.UntrackedChange(entity, ChangeHistoryAction.Update);
            }
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        internal void UntrackedDelete(TEntity entity)
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

            if (this.UntrackedChange != null)
            {
                this.UntrackedChange(entity, ChangeHistoryAction.Delete);
            }
        }

        #region Cleanup

        /// <summary>
        /// Releases all resources used by this SynchronisedRepository.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.TrackedChange = null;
                this.UntrackedChange = null;

                __lock.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}

