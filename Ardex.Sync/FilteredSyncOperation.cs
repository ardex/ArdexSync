using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    public class FilteredSyncOperation<TAnchor, TDelta> : BasicSyncOperation<TAnchor, TDelta>
    {
        /// <summary>
        /// Optional filter applied to the change
        /// delta returned by the source. Filters
        /// and/or transforms the changes before
        /// they are accepted by the target.
        /// </summary>
        public SyncFilter<TDelta> Filter { get; private set; }

        public FilteredSyncOperation(
            ISyncSource<TAnchor, TDelta> source,
            ISyncTarget<TAnchor, TDelta> target,
            SyncFilter<TDelta> filter) : base(source, target)
        {
            this.Filter = filter;
        }

        protected override Delta<TAnchor, TDelta> ResolveDelta(TAnchor anchor, System.Threading.CancellationToken ct)
        {
            var delta = base.ResolveDelta(anchor, ct);

            return new Delta<TAnchor, TDelta>(delta.Anchor, this.Filter(delta.Changes));
        }
    }
}
