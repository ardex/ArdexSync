using System.Collections.Generic;

namespace Ardex.Sync
{
    public static class SyncDelta
    {
        public static SyncDelta<TEntity, TAnchor, TVersion> Create<TEntity, TAnchor, TVersion>(TAnchor anchor, IEnumerable<SyncEntityVersion<TEntity, TVersion>> changes)
        {
            return new SyncDelta<TEntity, TAnchor, TVersion>(anchor, changes);
        }
    }

    public class SyncDelta<TEntity, TAnchor, TVersion>
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
