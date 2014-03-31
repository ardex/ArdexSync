using System;
using System.Collections.Generic;
using System.Linq;

using Ardex.Caching;
using Ardex.Collections.Generic;
using Ardex.Sync.SyncLocks;

namespace Ardex.Sync
{
    /// <summary>
    /// IRepository implementation which supports locking (used in sync operations).
    /// </summary>
    public class SyncRepository<TKey, TEntity>
        : KeyRepository<TKey, TEntity>
        , ISyncRepository<TKey, TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Backing field for Lock.
        /// </summary>
        private readonly ISyncLock __syncLock;

        /// <summary>
        /// Cached materialised collection of underlying values
        /// used as an optimisation for GetEnumerator().
        /// </summary>
        private readonly ICache<IReadOnlyList<TEntity>> CachedSnapshot;

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
                lock (this.Entities)
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
            : this(Enumerable.Empty<TEntity>(), keySelector, syncLock)
        {
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
            this.CachedSnapshot = new LazyCache<IReadOnlyList<TEntity>>(this.Snapshot);
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
                lock (this.Entities)
                {
                    this.Entities.Add(this.KeySelector(entity), entity);
                }
            }

            // Invalidate snapshot.
            this.CachedSnapshot.Invalidate();
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
                // Entity key validation.
                // Fail if the entity key has changed.
                var entityKey = this.KeySelector(entity);

                if (entity != this.Find(entityKey))
                {
                    throw new InvalidOperationException("Illegal entity key change detected.");
                }
            }

            // No need to invalidate snapshot
            // as the collection has not changed.

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
                lock (this.Entities)
                {
                    this.Entities.Remove(this.KeySelector(entity));
                }
            }

            // Invalidate snapshot.
            this.CachedSnapshot.Invalidate();
            this.OnEntityDeleted(entity);

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
            // Enumerator over cached clone.
            return this.CachedSnapshot.Value.GetEnumerator();
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        void ISyncRepository<TKey, TEntity>.UntrackedInsert(TEntity entity)
        {
            this.ThrowIfDisposed();

            lock (this.Entities)
            {
                this.Entities.Add(this.KeySelector(entity), entity);
            }

            // Invalidate snapshot.
            this.CachedSnapshot.Invalidate();
            this.OnEntityInserted(entity);

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
            this.ThrowIfDisposed();

            // Entity key validation.
            // Fail if the entity key has changed.
            var entityKey = this.KeySelector(entity);

            if (entity != this.Find(entityKey))
            {
                throw new InvalidOperationException("Illegal entity key change detected.");
            }

            // No need to invalidate snapshot
            // as the collection has not changed.

            this.OnEntityUpdated(entity);

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
            this.ThrowIfDisposed();

            lock (this.Entities)
            {
                this.Entities.Remove(this.KeySelector(entity));
            }

            // Invalidate snapshot.
            this.CachedSnapshot.Invalidate();
            this.OnEntityDeleted(entity);

            if (this.UntrackedChange != null)
            {
                this.UntrackedChange(entity, SyncEntityChangeAction.Delete);
            }
        }

        /// <summary>
        /// Returns the element with the specified
        /// key, or the default value for type.
        /// </summary>
        public override bool TryFind(TKey key, out TEntity entity)
        {
            lock (this.Entities)
            {
                return base.TryFind(key, out entity);
            }
        }

        #region Snapshot

        /// <summary>
        /// Returns a snapshot of the respository
        /// avoiding SyncDeadlockExceptions.
        /// </summary>
        protected virtual IReadOnlyList<TEntity> Snapshot()
        {
            lock (this.Entities)
            {
                return this.Entities.Values.ToList();
            }
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