using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Target of a sync operation.
    /// </summary>
    public interface ISyncTarget<TAnchor, TChange> : ISyncAnchor<TAnchor>
    {
        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        SyncResult AcceptChanges(SyncID sourceReplicaID, SyncDelta<TAnchor, TChange> delta, CancellationToken ct);
    }
}
