using System;

using Ardex.Sync.Providers.ChangeBased;

namespace Ardex.Sync.ChangeTracking
{
    /// <summary>
    /// Contract for a repository which
    /// supports locking and change tracking.
    /// </summary>
    public interface ISyncRepositoryWithChangeTracking<TEntity, TChangeHistory> : ISyncRepository<TEntity>
    {
        ///// <summary>
        ///// Creates a change history entry in response
        ///// to a locally-triggered entity change.
        ///// </summary>
        //Action<TEntity, ChangeHistoryAction> CreateChangeHistoryEntry { get; set; }

        /// <summary>
        /// Processes the remote change history entry.
        /// </summary>
        Action<TEntity, TChangeHistory> ProcessRemoteChangeHistoryEntry { get; set; }
    }
}
