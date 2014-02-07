using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync
{
    public static class SyncConflict
    {
        public static SyncConflict<TEntity> Create<TEntity>(TEntity local, TEntity remote)
        {
            return new SyncConflict<TEntity>(local, remote);
        }
    }

    public class SyncConflict<TEntity>
    {
        public TEntity Local { get; private set; }
        public TEntity Remote { get; private set; }

        public SyncConflict(TEntity local, TEntity remote)
        {
            this.Local = local;
            this.Remote = remote;
        }
    }
}
