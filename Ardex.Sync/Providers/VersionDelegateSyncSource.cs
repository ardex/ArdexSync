using System;
using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Timestamp-based sync provider which uses
    /// a custom ProduceChanges implementation.
    /// </summary>
    public class VersionDelegateSyncSource<TEntity> : ISyncSource<IComparable, TEntity>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Produces entities for diff sync after the given version.
        /// </summary>
        private readonly Func<IComparable, CancellationToken, IEnumerable<TEntity>> GetChanges;

        public VersionDelegateSyncSource(SyncID replicaID, Func<IComparable, CancellationToken, IEnumerable<TEntity>> getChanges)
        {
            this.ReplicaID = replicaID;
            this.GetChanges = getChanges;
        }

        public Delta<IComparable, TEntity> ResolveDelta(IComparable lastKnownVersion, CancellationToken ct)
        {
            var anchor = this.LastAnchor();
            var changes = this.GetChanges(lastKnownVersion, ct);

            return new Delta<IComparable, TEntity>(anchor, changes);
        }

        public IComparable LastAnchor()
        {
            return null;
        }
    }
}
