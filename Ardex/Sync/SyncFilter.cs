using System.Collections.Generic;

namespace Ardex.Sync
{
    /// <summary>
    /// Filter applied to entities/changes exchanged during the synchronisation.
    /// Can be used for filtering and/or transformation.
    /// </summary>
    public delegate IEnumerable<TEntity> SyncFilter<TEntity>(IEnumerable<TEntity> entities);
}
