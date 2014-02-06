using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Target of a sync operation.
    /// </summary>
    public interface ISyncTarget<TAnchor, TChange>
    {
        /// <summary>
        /// Retrieves the last anchor containing
        /// this replica's latest change knowledge.
        /// It is used by the source in order to detect
        /// the change delta that needs to be transferred.
        /// </summary>
        TAnchor LastAnchor();

        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        SyncResult AcceptChanges(SyncID sourceReplicaID, IEnumerable<TChange> changes, CancellationToken ct);
    }
}
