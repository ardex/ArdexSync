using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Source of a sync operation.
    /// </summary>
    public interface ISyncSource<TEntity, TVersion> : ISyncAnchor<TVersion>
    {
        /// <summary>
        /// Gets this replica's unique identifier.
        /// </summary>
        SyncID ReplicaID { get; }

        /// <summary>
        /// Resolves the changes made since the last reported anchor.
        /// </summary>
        SyncDelta<TEntity, TVersion> ResolveDelta(Dictionary<SyncID, TVersion> anchor, CancellationToken ct);
    }
}
