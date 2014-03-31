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
        /// Raised after an insert, update or delete.
        /// </summary>
        public event SyncRepositoryChangeEventHandler<TEntity> Changed;

        /// <summary>
        /// Raises the Changed event.
        /// </summary>
        public virtual void OnChanged(TEntity entity, SyncEntityChangeAction changeAction, SyncRepositoryChangeMode changeMode)
        {
            if (this.Changed != null)
            {
                var args = new SyncRepositoryChangeEventArgs<TEntity>(entity, changeAction, changeMode);

                this.Changed(this, args);
            }
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        public sealed override void Insert(TEntity entity)
        {
            this.Insert(entity, SyncRepositoryChangeMode.Tracked);
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        public sealed override void Update(TEntity entity)
        {
            this.Update(entity, SyncRepositoryChangeMode.Tracked);
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        public sealed override void Delete(TEntity entity)
        {
            this.Delete(entity, SyncRepositoryChangeMode.Tracked);
        }

        /// <summary>
        /// Inserts the specified entity.
        /// </summary>
        public virtual void Insert(TEntity entity, SyncRepositoryChangeMode changeMode)
        {
            this.ThrowIfDisposed();

            // No need for the lock for untracked changes.
            using (changeMode == SyncRepositoryChangeMode.Untracked ? null : __syncLock.WriteLock())
            {
                // No validation required for insert.
                // The dictionary will ensure 
                // that the key is unique.

                lock (this.Entities)
                {
                    this.Entities.Add(this.KeySelector(entity), entity);
                }
            }

            // Invalidate snapshot.
            this.CachedSnapshot.Invalidate();

            // Raise events.
            this.OnEntityInserted(entity);
            this.OnChanged(entity, SyncEntityChangeAction.Insert, changeMode);
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        public virtual void Update(TEntity entity, SyncRepositoryChangeMode changeMode)
        {
            this.ThrowIfDisposed();

            // We're not doing anything to the collection, but
            // still want an exclusive lock on the repository.

            // No need for the lock for untracked changes.
            using (changeMode == SyncRepositoryChangeMode.Untracked ? null : __syncLock.WriteLock())
            {
                // Additional validation.
                this.ValidateUpdate(entity);
            }

            // No need to invalidate snapshot
            // as the collection has not changed.

            // Raise events.
            this.OnEntityUpdated(entity);
            this.OnChanged(entity, SyncEntityChangeAction.Update, changeMode);
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        public virtual void Delete(TEntity entity, SyncRepositoryChangeMode changeMode)
        {
            this.ThrowIfDisposed();

            // No need for the lock for untracked changes.
            using (changeMode == SyncRepositoryChangeMode.Untracked ? null : __syncLock.WriteLock())
            {
                // Additional validation.
                this.ValidateDelete(entity);

                lock (this.Entities)
                {
                    this.Entities.Remove(this.KeySelector(entity));
                }
            }

            // Invalidate snapshot.
            this.CachedSnapshot.Invalidate();

            // Raise events.
            this.OnEntityDeleted(entity);
            this.OnChanged(entity, SyncEntityChangeAction.Delete, changeMode);
        }

        /// <summary>
        /// Pre-update validation.
        /// </summary>
        protected virtual void ValidateUpdate(TEntity entity)
        {
            var key = this.KeySelector(entity);
            var entityAtKey = this.Find(key);

            if (entityAtKey == null)
            {
                throw new InvalidOperationException(
                    "Update validation failed: entity with the matching key was not found in the repository."
                );
            }

            if (entity != entityAtKey)
            {
                throw new InvalidOperationException(
                    "Update validation failed: there is another entity with the given key."
                );
            }
        }

        /// <summary>
        /// Pre-delete validation.
        /// </summary>
        protected virtual void ValidateDelete(TEntity entity)
        {
            var key = this.KeySelector(entity);
            var entityAtKey = this.Find(key);

            if (entityAtKey == null)
            {
                throw new InvalidOperationException(
                    "Delete validation failed: entity with the matching key was not found in the repository."
                );
            }

            if (entity != this.Find(key))
            {
                throw new InvalidOperationException(
                    "Delete validation failed: there is another entity with the given key."
                );
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
                this.Changed = null;

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