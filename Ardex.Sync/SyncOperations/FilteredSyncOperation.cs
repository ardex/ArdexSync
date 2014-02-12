using System.Collections.Generic;

namespace Ardex.Sync.SyncOperations
{
    public class FilteredSyncOperation<TEntity, TVersion> : BasicSyncOperation<TEntity, TVersion>
    {
        /// <summary>
        /// Optional filter applied to the change
        /// delta returned by the source. Filters
        /// and/or transforms the changes before
        /// they are accepted by the target.
        /// </summary>
        public SyncFilter<TEntity, TVersion> Filter { get; private set; }

        public FilteredSyncOperation(
            ISyncProvider<TEntity, TVersion> source,
            ISyncProvider<TEntity, TVersion> target,
            SyncFilter<TEntity, TVersion> filter) : base(source, target)
        {
            this.Filter = filter;
        }

        protected override SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor)
        {
            var delta = base.ResolveDelta(remoteAnchor);
            var filteredChanges = this.Filter(delta.Changes);

            return SyncDelta.Create(delta.ReplicaInfo, delta.Anchor, filteredChanges);
        }
    }
}
