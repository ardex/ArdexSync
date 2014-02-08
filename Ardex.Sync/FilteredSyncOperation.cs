using System.Threading;

namespace Ardex.Sync
{
    public class FilteredSyncOperation<TAnchor, TChange> : BasicSyncOperation<TAnchor, TChange>
    {
        /// <summary>
        /// Optional filter applied to the change
        /// delta returned by the source. Filters
        /// and/or transforms the changes before
        /// they are accepted by the target.
        /// </summary>
        public SyncFilter<TChange> Filter { get; private set; }

        public FilteredSyncOperation(
            ISyncSource<TAnchor, TChange> source,
            ISyncTarget<TAnchor, TChange> target,
            SyncFilter<TChange> filter) : base(source, target)
        {
            this.Filter = filter;
        }

        protected override SyncDelta<TAnchor, TChange> ResolveDelta(TAnchor anchor, CancellationToken ct)
        {
            var delta = base.ResolveDelta(anchor, ct);

            return new SyncDelta<TAnchor, TChange>(delta.Anchor, this.Filter(delta.Changes));
        }
    }
}
