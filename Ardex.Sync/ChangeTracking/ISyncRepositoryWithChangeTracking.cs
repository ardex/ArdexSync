using System;

using Ardex.Sync.Providers.ChangeBased;

namespace Ardex.Sync.ChangeTracking
{
    public interface ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory> : ISyncRepository<TEntity>
    {
        /// <summary>
        /// Generates change history entries for local changes.
        /// </summary>
        Action<TEntity, ChangeHistoryAction> LocalChangeHistoryFactory { get; set; }

        /// <summary>
        /// Produces a change history from an equivalent remote entry.
        /// </summary>
        Action<TEntity, TChangeHistory> RemoteChangeHistoryFactory { get; set; }
    }
}
