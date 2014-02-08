using System;
using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync.Providers.Simple
{
    /// <summary>
    /// Version-based sync provider which uses
    /// a custom ProduceChanges implementation.
    /// </summary>
    public class VersionDelegateSyncSource<TEntity> : ISyncSource<IComparable, VersionInfo<TEntity, IComparable>>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Produces entities for diff sync after the given version.
        /// </summary>
        private readonly Func<IComparable, CancellationToken, IEnumerable<VersionInfo<TEntity, IComparable>>> GetChanges;

        public VersionDelegateSyncSource(SyncID replicaID, Func<IComparable, CancellationToken, IEnumerable<VersionInfo<TEntity, IComparable>>> getChanges)
        {
            this.ReplicaID = replicaID;
            this.GetChanges = getChanges;
        }

        public SyncDelta<IComparable, VersionInfo<TEntity, IComparable>> ResolveDelta(IComparable lastKnownVersion, CancellationToken ct)
        {
            var anchor = this.LastAnchor();
            var changes = this.GetChanges(lastKnownVersion, ct);

            return new SyncDelta<IComparable, VersionInfo<TEntity, IComparable>>(anchor, changes);
        }

        public IComparable LastAnchor()
        {
            return null;
        }
    }
}
