using System.Collections.Generic;
using System.Linq;

namespace Ardex.Sync
{
    public static class SyncDelta
    {
        public static SyncDelta<TEntity, TVersion> Create<TEntity, TVersion>(
            SyncReplicaInfo replicaInfo, SyncAnchor<TVersion> anchor, IEnumerable<SyncEntityVersion<TEntity, TVersion>> changes)
        {
            return new SyncDelta<TEntity, TVersion>(replicaInfo, anchor, changes.ToArray());
        }
    }

    /// <summary>
    /// Encapsulates sync anchor and change information.
    /// </summary>
    public class SyncDelta<TEntity, TVersion>
    {
        /// <summary>
        /// ID of the replica which created this delta.
        /// </summary>
        public SyncReplicaInfo ReplicaInfo { get; private set; }

        /// <summary>
        /// Contains most up-to date change knowledge of a particular party.
        /// </summary>
        public SyncAnchor<TVersion> Anchor { get; private set; }

        /// <summary>
        /// Contains changes resolved for the other party after receiving anchor.
        /// </summary>
        public SyncEntityVersion<TEntity, TVersion>[] Changes { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncDelta(SyncReplicaInfo replicaInfo, SyncAnchor<TVersion> anchor, SyncEntityVersion<TEntity, TVersion>[] changes)
        {
            this.ReplicaInfo = replicaInfo;
            this.Anchor = anchor;
            this.Changes = changes;
        }
    }
}
