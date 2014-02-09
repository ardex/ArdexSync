using System.Collections.Generic;
using System.Linq;

namespace Ardex.Sync
{
    public static class SyncDelta
    {
        public static SyncDelta<TEntity, TVersion> Create<TEntity, TVersion>(SyncAnchor<TVersion> anchor, IEnumerable<SyncEntityVersion<TEntity, TVersion>> changes)
        {
            return new SyncDelta<TEntity, TVersion>(anchor, changes.ToArray());
        }
    }

    /// <summary>
    /// Encapsulates sync anchor and change information.
    /// </summary>
    public class SyncDelta<TEntity, TVersion>
    {
        /// <summary>
        /// Contains most up-to date change knowledge of a particular party.
        /// </summary>
        public SyncAnchor<TVersion> Anchor { get; private set; }

        /// <summary>
        /// Contains changes resolved for the other party after receiving anchor.
        /// </summary>
        public SyncEntityVersion<TEntity, TVersion>[] Changes { get; private set; }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public SyncDelta(SyncAnchor<TVersion> anchor, SyncEntityVersion<TEntity, TVersion>[] changes)
        {
            this.Anchor = anchor;
            this.Changes = changes;
        }
    }
}
