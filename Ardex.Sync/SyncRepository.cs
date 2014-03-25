using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Ardex.Collections.Generic;

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
        private readonly ReaderWriterLockSlim __lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Lock used to protect read and write
        /// operations in this repository.
        /// </summary>
        ReaderWriterLockSlim ISyncRepository<TEntity>.Lock
        {
            get { return __lock; }
        }

        /// <summary>
        /// Overridden to always return False.
        /// Event forwarding is not supported.
        /// </summary>
        public override bool ForwardEvents
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotSupportedException("Event forwarding is not supported by SyncRepository.");
            }
        }

        /// <summary>
        /// Gets the number of entities in the respository.
        /// </summary>
        public override int Count
        {
            get
            {
                using (this.ReadLock())
                {
                    return base.Count;
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
        public event Action<TEntity, SyncEntityChangeAction> TrackedChange;

        /// <summary>
        /// Raised after an untracked insert, update or delete.
        /// </summary>
        public event Action<TEntity, SyncEntityChangeAction> UntrackedChange;

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        public override void Insert(TEntity entity)
        {
            using (this.WriteLock())
            {
                base.Insert(entity);
            }

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, SyncEntityChangeAction.Insert);
            }
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        public override void Update(TEntity entity)
        {
            using (this.WriteLock())
            {
                base.Update(entity);
            }

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, SyncEntityChangeAction.Update);
            }
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        public override void Delete(TEntity entity)
        {
            using (this.WriteLock())
            {
                base.Delete(entity);
            }

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, SyncEntityChangeAction.Delete);
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
            using (this.ReadLock())
            {
                // We'll create a clone.
                var snapshot = this.InnerRepository.ToList();

                return snapshot.GetEnumerator();
            }
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        public void UntrackedInsert(TEntity entity)
        {
            base.Insert(entity);

            if (this.UntrackedChange != null)
            {
                this.UntrackedChange(entity, SyncEntityChangeAction.Insert);
            }
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        public void UntrackedUpdate(TEntity entity)
        {
            base.Update(entity);

            if (this.UntrackedChange != null)
            {
                this.UntrackedChange(entity, SyncEntityChangeAction.Update);
            }
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        public void UntrackedDelete(TEntity entity)
        {
            base.Delete(entity);

            if (this.UntrackedChange != null)
            {
                this.UntrackedChange(entity, SyncEntityChangeAction.Delete);
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