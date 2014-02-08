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

    /// <summary>
    /// Encapsulates sync anchor and change information.
    /// </summary>
    public class SyncDelta<TEntity, TVersion, TAnchor>
    {
        /// <summary>
        /// Contains most up-to date change knowledge of a particular party.
        /// </summary>
        public TAnchor Anchor { get; private set; }

        /// <summary>
        /// Contains changes resolved for the other party after receiving anchor.
        /// </summary>
        public IEnumerable<SyncEntityVersion<TEntity, TVersion>> Changes { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncDelta(TAnchor anchor, IEnumerable<SyncEntityVersion<TEntity, TVersion>> changes)
        {
            this.Anchor = anchor;
            this.Changes = changes;
        }
    }
}
