using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ardex.Collections.Generic;
using Ardex.Sync.SyncLocks;

namespace Ardex.Sync
{
    /// <summary>
    /// ProxyRepository implementation which supports locking (used in sync operations).
    /// </summary>
    public class SyncRepository<TKey, TEntity> : DictionaryRepository<TKey, TEntity>, ISyncRepository<TKey, TEntity>
    {
        /// <summary>
        /// Backing field for Lock.
        /// </summary>s
        private readonly ISyncLock __syncLock;

        /// <summary>
        /// Lock used to protect read and write
        /// operations in this repository.
        /// </summary>
        ISyncLock ISyncRepository<TKey, TEntity>.SyncLock
        {
            get { return __syncLock; }
        }

        /// <summary>
        /// Flag indicating that this instance
        /// owns (i.e. disposes of) the sync lock.
        /// </summary>
        private bool OwnsLock { get; set; }

        /// <summary>
        /// Gets the number of entities in the respository.
        /// </summary>
        public override int Count
        {
            get
            {
                using (__syncLock.ReadLock())
                {
                    return base.Count;
                }
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SyncRepository(Func<TEntity, TKey> keySelector)
            : this(keySelector, new ReaderWriterSyncLock())
        {
            this.OwnsLock = true;
        }

        /// <summary>
        /// Initialises a new instance pre-populated with the given entities.
        /// </summary>
        public SyncRepository(IEnumerable<TEntity> entities, Func<TEntity, TKey> keySelector)
            : this(entities, keySelector, new ReaderWriterSyncLock())
        {
            this.OwnsLock = true;
        }

        /// <summary>
        /// Initialises a new instance using the given lock object.
        /// </summary>
        public SyncRepository(Func<TEntity, TKey> keySelector, ISyncLock syncLock)
            : base(keySelector)
        {
            if (syncLock == null) throw new ArgumentNullException("syncLock");

            __syncLock = syncLock;

            this.OwnsLock = false;
        }

        /// <summary>
        /// Initialises a new instance pre-populated
        /// with the given entities and lock object.
        /// </summary>
        public SyncRepository(IEnumerable<TEntity> entities, Func<TEntity, TKey> keySelector, ISyncLock syncLock)
            : base(entities, keySelector)
        {
            if (syncLock == null) throw new ArgumentNullException("syncLock");

            __syncLock = syncLock;

            this.OwnsLock = false;
        }

        /// <summary>
        /// Raised after a tracked insert, update or delete.
        /// </summary>
        public event Action<TEntity, SyncEntityChangeAction> TrackedChange;

        /// <summary>
        /// Raised after an untracked insert, update or delete.
        /// </summary>
        public event Action<TEntity, SyncEntityChangeAction> UntrackedChange;

        /// <summary>
        /// Insert the specified entity.
        /// </summary>
        public override void Insert(TEntity entity)
        {
            this.ThrowIfDisposed();

            using (__syncLock.WriteLock())
            {
                this.Entities.Add(this.KeySelector(entity), entity);
            }

            this.OnEntityInserted(entity);

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, SyncEntityChangeAction.Insert);
            }
        }

        /// <summary>
        /// Update the specified entity.
        /// </summary>
        public override void Update(TEntity entity)
        {
            this.ThrowIfDisposed();

            // We're not doing anything
            // to the collection, but
            // still need an exclusive lock.
            using (__syncLock.WriteLock())
            {
                // Key validation?
            }

            this.OnEntityUpdated(entity);

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, SyncEntityChangeAction.Update);
            }
        }

        /// <summary>
        /// Delete the specified entity.
        /// </summary>
        public override void Delete(TEntity entity)
        {
            this.ThrowIfDisposed();

            using (__syncLock.WriteLock())
            {
                this.Entities.Remove(this.KeySelector(entity));
            }

            this.OnEntityDeleted(entity);

            if (this.TrackedChange != null)
            {
                this.TrackedChange(entity, SyncEntityChangeAction.Delete);
            }
        }

        /// <summary>
        /// Returns a snapshot of the respository
        /// avoiding SyncDeadlockExceptions.
        /// </summary>
        protected List<TEntity> Snapshot()
        {
            while (true)
            {
                try
                {
                    var list = this.Entities
                        .Select(kvp => kvp.Value)
                        .ToList();

                    if (list.Count == this.Entities.Count)
                    {
                        return list;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // Collection was modified.
                    if (ex.Message != null &&
                        ex.Message.StartsWith("Collection was modified", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("SyncRepository<{0}>.Snapshot(): InvalidOperationException caught: {1}", typeof(TEntity).Name, ex.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (ArgumentException ex)
                {
                    // Collection was modified.
                    if (ex.Message != null &&
                        ex.Message.StartsWith("Destination array was not long enough.", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("SyncRepository<{0}>.Snapshot(): ArgumentException caught: {1}", typeof(TEntity).Name, ex.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
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
            //using (this.ReadLock())
            {
                // We'll create a clone.
                var snapshot = this.Snapshot();

                return snapshot.GetEnumerator();
            }
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        void ISyncRepository<TKey, TEntity>.UntrackedInsert(TEntity entity)
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
        void ISyncRepository<TKey, TEntity>.UntrackedUpdate(TEntity entity)
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
        void ISyncRepository<TKey, TEntity>.UntrackedDelete(TEntity entity)
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

                if (this.OwnsLock)
                {
                    __syncLock.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}