using System.Collections.Generic;

namespace Ardex.Sync
{
    /// <summary>
    /// Filter applied to entities/changes exchanged during the synchronisation.
    /// Can be used for filtering and/or transformation.
    /// </summary>
    public delegate IEnumerable<SyncEntityVersion<TEntity, TVersion>> SyncFilter<TEntity, TVersion>(IEnumerable<SyncEntityVersion<TEntity, TVersion>> changes);
}