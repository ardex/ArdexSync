using System.Collections.Generic;

namespace Ardex.Sync
{
    public static class SyncDelta
    {
        public static SyncDelta<TEntity, TVersion, TAnchor> Create<TEntity, TVersion, TAnchor>(TAnchor anchor, IEnumerable<SyncEntityVersion<TEntity, TVersion>> changes)
        {
            return new SyncDelta<TEntity, TVersion, TAnchor>(anchor, changes);
        }
    }

    public class SyncDelta<TEntity, TVersion, TAnchor>
    {
        public TAnchor Anchor { get; private set; }
        public IEnumerable<SyncEntityVersion<TEntity, TVersion>> Changes { get; private set; }

        public SyncDelta(TAnchor anchor, IEnumerable<SyncEntityVersion<TEntity, TVersion>> changes)
        {
            this.Anchor = anchor;
            this.Changes = changes;
        }
    }
}
