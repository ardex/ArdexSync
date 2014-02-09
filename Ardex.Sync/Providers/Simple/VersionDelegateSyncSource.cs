using System;
using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync.Providers.Simple
{
    /// <summary>
    /// Version-based sync provider which uses
    /// a custom ProduceChanges implementation.
    /// </summary>
    public class VersionDelegateSyncSource<TEntity> : ISyncSource<TEntity, IComparable>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Produces entities for diff sync after the given version.
        /// </summary>
        private readonly Func<SyncAnchor<IComparable>, CancellationToken, IEnumerable<SyncEntityVersion<TEntity, IComparable>>> GetChanges;

        public VersionDelegateSyncSource(SyncID replicaID, Func<SyncAnchor<IComparable>, CancellationToken, IEnumerable<SyncEntityVersion<TEntity, IComparable>>> getChanges)
        {
            this.ReplicaID = replicaID;
            this.GetChanges = getChanges;
        }

        public SyncDelta<TEntity, IComparable> ResolveDelta(SyncAnchor<IComparable> lastKnownVersion, CancellationToken ct)
        {
            var anchor = this.LastAnchor();
            var changes = this.GetChanges(lastKnownVersion, ct);

            return SyncDelta.Create(anchor, changes);
        }

        public SyncAnchor<IComparable> LastAnchor()
        {
            return null;
        }
    }
}
