using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ardex.Sync
{
    /// <summary>
    /// Basic sync operation implementation.
    /// </summary>
    public class BasicSyncOperation<TAnchor, TChange> : SyncOperation
    {
        #region Transformation methods

        public FilteredSyncOperation<TAnchor, TChange> Filtered(SyncFilter<TChange> filter)
        {
            return new FilteredSyncOperation<TAnchor, TChange>(this.Source, this.Target, filter);
        }

        #endregion

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
            var anchor = this.LastAnchor();
            ct.ThrowIfCancellationRequested();

            var delta = this.ResolveDelta(anchor, ct);
            ct.ThrowIfCancellationRequested();

            return this.AcceptChanges(delta, ct);
        }

        protected virtual TAnchor LastAnchor()
        {
            return this.Target.LastAnchor();
        }

        protected virtual Delta<TAnchor, TChange> ResolveDelta(TAnchor anchor, CancellationToken ct)
        {
            return this.Source.ResolveDelta(anchor, ct);
        }

        protected virtual SyncResult AcceptChanges(Delta<TAnchor, TChange> delta, CancellationToken ct)
        {
            var changes = delta.Changes;

            // See if either source or target support metadata cleanup.
            var sourceCleanup = this.Source as ISyncMetadataCleanup<TChange>;
            var targetCleanup = this.Target as ISyncMetadataCleanup<TChange>;

            if (sourceCleanup != null || targetCleanup != null)
            {
                // Materialise changes since they will
                // need to be iterated over multiple times.
                changes = changes.ToArray();
                ct.ThrowIfCancellationRequested();
            }

            // Accept changes.
            var result = this.Target.AcceptChanges(this.Source.ReplicaID, delta, ct);
            ct.ThrowIfCancellationRequested();

            // Source sync metdatata cleanup.
            if (sourceCleanup != null) sourceCleanup.CleanUpSyncMetadata(changes);
            ct.ThrowIfCancellationRequested();

            // Target sync metdatata cleanup.
            if (targetCleanup != null) targetCleanup.CleanUpSyncMetadata(changes);
            ct.ThrowIfCancellationRequested();

            return result;
        }
    }
}
