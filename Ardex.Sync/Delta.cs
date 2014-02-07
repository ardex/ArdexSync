using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    public class Delta<TAnchor, TChange>
    {
        public TAnchor Anchor { get; private set; }
        public IEnumerable<TChange> Changes { get; private set; }

        public Delta(TAnchor anchor, IEnumerable<TChange> changes)
        {
            this.Anchor = anchor;
            this.Changes = changes;
        }
    }
}
