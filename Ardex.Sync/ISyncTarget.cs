using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Target of a sync operation.
    /// </summary>
    public interface ISyncTarget<TEntity, TAnchor, TVersion> : ISyncAnchor<TAnchor>
    {
        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        SyncResult AcceptChanges(SyncID sourceReplicaID, SyncDelta<TEntity, TAnchor, TVersion> delta, CancellationToken ct);
    }
}
