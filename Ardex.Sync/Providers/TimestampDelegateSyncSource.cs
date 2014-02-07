using System;
using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Timestamp-based sync provider which uses
    /// a custom ProduceChanges implementation.
    /// </summary>
    public class TimestampDelegateSyncSource<TEntity> : ISyncSource<IComparable, TEntity>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Produces entities for diff sync after the given timestamp.
        /// </summary>
        private readonly Func<IComparable, CancellationToken, IEnumerable<TEntity>> GetChanges;

        public TimestampDelegateSyncSource(SyncID replicaID, Func<IComparable, CancellationToken, IEnumerable<TEntity>> getChanges)
        {
            this.ReplicaID = replicaID;
            this.GetChanges = getChanges;
        }

        public Delta<IComparable, TEntity> ResolveDelta(IComparable lastSeenTimestamp, CancellationToken ct)
        {
            var anchor = this.LastAnchor();
            var changes = this.GetChanges(lastSeenTimestamp, ct);

            return new Delta<IComparable, TEntity>(anchor, changes);
        }

        public IComparable LastAnchor()
        {
            return null;
        }
    }
}
