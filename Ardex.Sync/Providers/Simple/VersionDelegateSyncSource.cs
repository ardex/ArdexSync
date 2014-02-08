using System;
using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync.Providers.Simple
{
    /// <summary>
    /// Version-based sync provider which uses
    /// a custom ProduceChanges implementation.
    /// </summary>
    public class VersionDelegateSyncSource<TEntity> : ISyncSource<TEntity, IComparable, IComparable>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Produces entities for diff sync after the given version.
        /// </summary>
        private readonly Func<IComparable, CancellationToken, IEnumerable<SyncEntityVersion<TEntity, IComparable>>> GetChanges;

        public VersionDelegateSyncSource(SyncID replicaID, Func<IComparable, CancellationToken, IEnumerable<SyncEntityVersion<TEntity, IComparable>>> getChanges)
        {
            this.ReplicaID = replicaID;
            this.GetChanges = getChanges;
        }

        public SyncDelta<TEntity, IComparable, IComparable> ResolveDelta(IComparable lastKnownVersion, CancellationToken ct)
        {
            var anchor = this.LastAnchor();
            var changes = this.GetChanges(lastKnownVersion, ct);

            return SyncDelta.Create(anchor, changes);
        }

        public IComparable LastAnchor()
        {
            return null;
        }
    }
}
