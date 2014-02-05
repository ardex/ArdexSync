using System;
using System.Collections.Generic;

using Ardex.Collections;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// SyncRepository with provisions for integrated change tracking.
    /// </summary>
    public class SyncRepositoryWithChangeTracking<TEntity> : SyncRepository<TEntity>, ISyncRepositoryWithChangeTracking<TEntity>
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SyncRepositoryWithChangeTracking() : base() { }

        /// <summary>
        /// Initialises a new instance wrapping the given repository.
        /// </summary>
        public SyncRepositoryWithChangeTracking(IRepository<TEntity> repository) : base(repository) { }

        /// <summary>
        /// Initialises a new instance pre-populated with the given entities.
        /// </summary>
        public SyncRepositoryWithChangeTracking(IEnumerable<TEntity> entities) : base(entities) { }

        /// <summary>
        /// Inserts the given entity and creates change tracking entries.
        /// </summary>
        public override void Insert(TEntity entity)
        {
            var repo = this.AsSyncRepository();

            if (repo.TrackInsert == null)
            {
                throw new InvalidOperationException("Insert tracking has not been set up.");
            }
            
            repo.ObtainExclusiveLock();
            
            try
            {
                repo.DirectInsert(entity);
                repo.TrackInsert(entity);
            }
            finally
            {
                repo.ReleaseExclusiveLock();
            }
        }

        /// <summary>
        /// Updates the given entity and creates change tracking entries.
        /// </summary>
        public override void Update(TEntity entity)
        {
            var repo = this.AsSyncRepository();

            if (repo.TrackUpdate == null)
            {
                throw new InvalidOperationException("Update tracking has not been set up.");
            }

            repo.ObtainExclusiveLock();

            try
            {
                repo.DirectUpdate(entity);
                repo.TrackUpdate(entity);
            }
            finally
            {
                repo.ReleaseExclusiveLock();
            }
        }

        /// <summary>
        /// Deletes the given entity and creates change tracking entries.
        /// </summary>
        public override void Delete(TEntity entity)
        {
            var repo = this.AsSyncRepository();

            if (repo.TrackDelete == null)
            {
                throw new InvalidOperationException("Delete tracking has not been set up.");
            }

            repo.ObtainExclusiveLock();

            try
            {
                repo.DirectDelete(entity);
                repo.TrackDelete(entity);
            }
            finally
            {
                repo.ReleaseExclusiveLock();
            }
        }

        /// <summary>
        /// Performs a cast of this instance to a generic
        /// ISyncRepository and returns the result.
        /// </summary>
        private ISyncRepositoryWithChangeTracking<TEntity> AsSyncRepository()
        {
            return this;
        }

        // Change tracking provisions.
        Action<TEntity> ISyncRepositoryWithChangeTracking<TEntity>.TrackInsert { get; set; }
        Action<TEntity> ISyncRepositoryWithChangeTracking<TEntity>.TrackUpdate { get; set; }
        Action<TEntity> ISyncRepositoryWithChangeTracking<TEntity>.TrackDelete { get; set; }
    }
}
