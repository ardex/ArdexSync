using System.Collections.Generic;

namespace Ardex.Sync
{
    public static class SyncDelta
    {
        public static SyncDelta<TAnchor, TChange> Create<TAnchor, TChange>(TAnchor anchor, IEnumerable<TChange> changes)
        {
            return new SyncDelta<TAnchor, TChange>(anchor, changes);
        }
    }

    public class SyncDelta<TAnchor, TChange>
    {
        public TAnchor Anchor { get; private set; }
        public IEnumerable<TChange> Changes { get; private set; }

        public SyncDelta(TAnchor anchor, IEnumerable<TChange> changes)
        {
            this.Anchor = anchor;
            this.Changes = changes;
        }
    }
}
