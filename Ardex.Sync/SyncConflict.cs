using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    public class SyncConflict<TEntity>
    {
        public TEntity Winner { get; private set; }
        public TEntity Loser { get; private set; }

        public SyncConflict(TEntity winner, TEntity loser)
        {
            this.Winner = winner;
            this.Loser = loser;
        }
    }
}
