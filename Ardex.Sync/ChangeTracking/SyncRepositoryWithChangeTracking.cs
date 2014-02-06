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
        Action<TEntity, TChangeHistory> ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory>.ProcessRemoteChangeHistoryEntry { get; set; }

        /// <summary>
        /// Performs a cast of this instance to a generic
        /// ISyncRepository and returns the result.
        /// </summary>
        private ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory> AsSyncRepository()
        {
            return this;
        }
    }
}
