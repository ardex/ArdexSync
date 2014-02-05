using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ardex.Sync.ChangeBased
{
    /// <summary>
    /// Sync operation based on change history metadata.
    /// </summary>
    public class ChangeSync<TEntity> : SyncOperation
    {
        // Parameters.
        public IChangeSyncSource<TEntity> Source { get; private set; }
        public IChangeSyncTarget<TEntity> Target { get; private set; }

        // Properties.
        public SyncFilter<Change<TEntity>> Filter { get; set; }

        /// <summary>
        /// Maximum change batch size (default = 0, unlimited).
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public ChangeSync(IChangeSyncSource<TEntity> source, IChangeSyncTarget<TEntity> Target)
        {
            this.Source = source;
            this.Target = Target;
           
            // Defaults.
            this.BatchSize = 0; // Unlimited.
        }

        /// <summary>
        /// Sync implementation.
        /// </summary>
        protected override SyncResult SynchroniseDiff(CancellationToken ct)
        {
            var lastSeenTimestamps = this.Target.LastAnchor();

            ct.ThrowIfCancellationRequested();

            var changes = this.Source.ResolveDelta(lastSeenTimestamps, this.BatchSize, ct);

            ct.ThrowIfCancellationRequested();

            // Apply filter if it is a filtered sync operation.
            if (this.Filter != null)
            {
                changes = this.Filter(changes);
            }

            // Materialise changes and
            // Ensure that the oldest changes for each node sync first.
            changes = changes
                .OrderBy(c => c.ChangeHistory.Timestamp)
                .ToArray();

            ct.ThrowIfCancellationRequested();
            
            var result = this.Target.AcceptChanges(this.Source.ReplicaID, changes, ct);

            // Now that the changes have been accepted, clear out change history entries.
            var cleanup1 = this.Source as IChangeHistoryCleanup<TEntity>;
            var cleanup2 = this.Target as IChangeHistoryCleanup<TEntity>;

            if (cleanup1 != null) cleanup1.CleanUpMetadata(changes);
            if (cleanup2 != null) cleanup2.CleanUpMetadata(changes);

            return result;
        }
    }
}