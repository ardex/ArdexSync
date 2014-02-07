using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        protected override IEnumerable<TChange> ResolveDelta(TAnchor anchor, System.Threading.CancellationToken ct)
        {
            var delta = base.ResolveDelta(anchor, ct);

            return this.Filter(delta);
        }
    }
}
