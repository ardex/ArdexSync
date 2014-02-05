using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ardex.Sync.TimestampBased
{
    /// <summary>
    /// Timestamp-based sync provider which uses
    /// a custom ProduceChanges implementation.
    /// </summary>
    public class TimestampSyncDelegateSource<TEntity> : ISyncSource<Timestamp, TEntity>
    {
        /// <summary>
        /// Unique identifier of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Produces entities for diff sync after the given timestamp.
        /// </summary>
        private readonly Func<Timestamp, CancellationToken, IEnumerable<TEntity>> GetChanges;

        public TimestampSyncDelegateSource(SyncID replicaID, Func<Timestamp, CancellationToken, IEnumerable<TEntity>> getChanges)
        {
            this.ReplicaID = replicaID;
            this.GetChanges = getChanges;
        }

        public IEnumerable<TEntity> ResolveDelta(Timestamp lastSeenTimestamp, int batchSize, CancellationToken ct)
        {
            var changes = this.GetChanges(lastSeenTimestamp, ct);

            if (batchSize != 0)
            {
                changes = changes.Take(batchSize);
            }

            return changes;
        }
    }
}
