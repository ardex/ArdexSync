using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Source of a sync operation.
    /// </summary>
    public interface ISyncSource<TAnchor, TChange> : ISyncAnchor<TAnchor>
    {
        /// <summary>
        /// Gets this replica's unique identifier.
        /// </summary>
        SyncID ReplicaID { get; }

        /// <summary>
        /// Resolves the changes made since the last reported anchor.
        /// </summary>
        Delta<TAnchor, TChange> ResolveDelta(TAnchor anchor, CancellationToken ct);
    }
}
