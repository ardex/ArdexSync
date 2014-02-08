﻿using System.Threading;

namespace Ardex.Sync.SyncOperations
{
    public class FilteredSyncOperation<TEntity, TVersion, TAnchor> : BasicSyncOperation<TEntity, TVersion, TAnchor>
    {
        /// <summary>
        /// Optional filter applied to the change
        /// delta returned by the source. Filters
        /// and/or transforms the changes before
        /// they are accepted by the target.
        /// </summary>
        public SyncFilter<TEntity, TVersion> Filter { get; private set; }

        public FilteredSyncOperation(
            ISyncSource<TEntity, TVersion, TAnchor> source,
            ISyncTarget<TEntity, TVersion, TAnchor> target,
            SyncFilter<TEntity, TVersion> filter) : base(source, target)
        {
            this.Filter = filter;
        }

        protected override SyncDelta<TEntity, TVersion, TAnchor> ResolveDelta(TAnchor anchor, CancellationToken ct)
        {
            var delta = base.ResolveDelta(anchor, ct);

            return SyncDelta.Create(delta.Anchor, this.Filter(delta.Changes));
        }
    }
}