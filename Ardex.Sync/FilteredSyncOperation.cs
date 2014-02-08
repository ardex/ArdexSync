using System.Threading;

namespace Ardex.Sync
{
    public class FilteredSyncOperation<TEntity, TAnchor, TVersion> : BasicSyncOperation<TEntity, TAnchor, TVersion>
    {
        /// <summary>
        /// Optional filter applied to the change
        /// delta returned by the source. Filters
        /// and/or transforms the changes before
        /// they are accepted by the target.
        /// </summary>
        public SyncFilter<TEntity, TVersion> Filter { get; private set; }

        public FilteredSyncOperation(
            ISyncSource<TEntity, TAnchor, TVersion> source,
            ISyncTarget<TEntity, TAnchor, TVersion> target,
            SyncFilter<TEntity, TVersion> filter) : base(source, target)
        {
            this.Filter = filter;
        }

        protected override SyncDelta<TEntity, TAnchor, TVersion> ResolveDelta(TAnchor anchor, CancellationToken ct)
        {
            var delta = base.ResolveDelta(anchor, ct);

            return SyncDelta.Create(delta.Anchor, this.Filter(delta.Changes));
        }
    }
}
