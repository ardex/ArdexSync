using System;
using System.Collections.Generic;

using Ardex.Collections;
using Ardex.Sync.Providers.ChangeBased;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// SyncRepository with provisions for integrated change tracking.
    /// </summary>
    public class SyncRepositoryWithChangeTracking<TEntity, TChangeHistory> : SyncRepository<TEntity>, ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory>
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
        /// Generates change history entries.
        /// </summary>
        Action<TEntity, ChangeHistoryAction> ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory>.LocalChangeHistoryFactory { get; set; }
        Action<TEntity, TChangeHistory> ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory>.RemoteChangeHistoryFactory { get; set; }

        /// <summary>
        /// Inserts the given entity and creates change tracking entries.
        /// </summary>
        public override void Insert(TEntity entity)
        {
            var repo = this.AsSyncRepository();

            if (repo.LocalChangeHistoryFactory == null)
            {
                throw new InvalidOperationException("Insert tracking has not been set up.");
            }
            
            repo.ObtainExclusiveLock();
            
            try
            {
                repo.DirectInsert(entity);

                // Generate change history.
                repo.LocalChangeHistoryFactory(entity, ChangeHistoryAction.Insert);
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

            if (repo.LocalChangeHistoryFactory == null)
            {
                throw new InvalidOperationException("Update tracking has not been set up.");
            }

            repo.ObtainExclusiveLock();

            try
            {
                repo.DirectUpdate(entity);

                // Generate change history.
                repo.LocalChangeHistoryFactory(entity, ChangeHistoryAction.Update);
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

            if (repo.LocalChangeHistoryFactory == null)
            {
                throw new InvalidOperationException("Delete tracking has not been set up.");
            }

            repo.ObtainExclusiveLock();

            try
            {
                repo.DirectDelete(entity);

                // Generate change history.
                repo.LocalChangeHistoryFactory(entity, ChangeHistoryAction.Delete);
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
        private ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory> AsSyncRepository()
        {
            return this;
        }

        //// Change tracking provisions.
        //Action<TChangeHistory> ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory>.TrackInsert { get; set; }
        //Action<TChangeHistory> ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory>.TrackUpdate { get; set; }
        //Action<TChangeHistory> ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory>.TrackDelete { get; set; }
    }
}
