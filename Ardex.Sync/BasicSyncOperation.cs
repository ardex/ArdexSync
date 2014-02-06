using System.Linq;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Basic sync operation implementation.
    /// </summary>
    public class BasicSyncOperation<TAnchor, TChange> : SyncOperation
    {
        /// <summary>
        /// Sync operation source or provider.
        /// Resolves the change delta in a differential sync operation
        /// based on anchor info provided by the sync target.
        /// </summary>
        public ISyncSource<TAnchor, TChange> Source { get; private set; }

        /// <summary>
        /// Sync operation target or provider.
        /// Produces anchor info for the
        /// source and accepts changes.
        /// </summary>
        public ISyncTarget<TAnchor, TChange> Target { get; private set; }

        /// <summary>
        /// Optional filter applied to the change
        /// delta returned by the source. Filters
        /// and/or transforms the changes before
        /// they are accepted by the target.
        /// </summary>
        public SyncFilter<TChange> Filter { get; set; }

        /// <summary>
        /// Creates a new SyncOperation instance.
        /// Consider using SyncOperation.Create for convenience.
        /// </summary>
        public BasicSyncOperation(ISyncSource<TAnchor, TChange> source, ISyncTarget<TAnchor, TChange> target)
        {
            this.Source = source;
            this.Target = target;
        }

        /// <summary>
        /// Differential synchronisation implementation.
        /// </summary>
        protected override SyncResult SynchroniseDiff(CancellationToken ct)
        {
            // Determine 
            var anchor = this.Target.LastAnchor();
            ct.ThrowIfCancellationRequested();

            var delta = this.Source.ResolveDelta(anchor, ct);
            ct.ThrowIfCancellationRequested();

            if (this.Filter != null)
            {
                delta = this.Filter(delta);
                ct.ThrowIfCancellationRequested();
            }

            // See if either source or target support metadata cleanup.
            var sourceCleanup = this.Source as ISyncMetadataCleanup<TChange>;
            var targetCleanup = this.Target as ISyncMetadataCleanup<TChange>;

            if (sourceCleanup != null || targetCleanup != null)
            {
                // Materialise changes since they will
                // need to be iterated over multiple times.
                delta = delta.ToArray();
                ct.ThrowIfCancellationRequested();
            }

            // Accept changes.
            var result = this.Target.AcceptChanges(this.Source.ReplicaID, delta, ct);
            ct.ThrowIfCancellationRequested();

            // Source sync metdatata cleanup.
            if (sourceCleanup != null) sourceCleanup.CleanUpSyncMetadata(delta);
            ct.ThrowIfCancellationRequested();

            // Target sync metdatata cleanup.
            if (targetCleanup != null) targetCleanup.CleanUpSyncMetadata(delta);
            ct.ThrowIfCancellationRequested();

            return result;
        }
    }
}
