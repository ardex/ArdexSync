using System;
using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Timestamp-based sync provider which uses
    /// a custom ProduceChanges implementation.
    /// </summary>
    public class TimestampDelegateSyncSource<TEntity> : ISyncSource<Timestamp, TEntity>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Produces entities for diff sync after the given timestamp.
        /// </summary>
        private readonly Func<Timestamp, CancellationToken, IEnumerable<TEntity>> GetChanges;

        public TimestampDelegateSyncSource(SyncID replicaID, Func<Timestamp, CancellationToken, IEnumerable<TEntity>> getChanges)
        {
            this.ReplicaID = replicaID;
            this.GetChanges = getChanges;
        }

        public IEnumerable<TEntity> ResolveDelta(Timestamp lastSeenTimestamp, CancellationToken ct)
        {
            return this.GetChanges(lastSeenTimestamp, ct);
        }
    }
}
