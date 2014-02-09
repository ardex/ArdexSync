using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    /// <summary>
    /// Complete sync provider, which includes both
    /// sync source and sync target functionality.
    /// </summary>
    public interface ISyncProvider<TEntity, TVersion>
    {
        /// <summary>
        /// Gets this replica's unique identifier.
        /// </summary>
        SyncID ReplicaID { get; }

        /// <summary>
        /// Retrieves the last anchor containing
        /// this replica's latest change knowledge.
        /// It is used by the other side in order to detect
        /// the change delta that needs to be transferred
        /// and to detect and resolve conflicts.
        /// </summary>
        SyncAnchor<TVersion> LastAnchor();

        /// <summary>
        /// Resolves the changes made since the last reported anchor.
        /// </summary>
        SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor, CancellationToken ct);

        /// <summary>
        /// Accepts the changes as reported by the given node.
        /// </summary>
        SyncResult AcceptChanges(SyncID sourceReplicaID, SyncDelta<TEntity, TVersion> remoteDelta, CancellationToken ct);
    }
}
