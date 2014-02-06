using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ardex.Sync.ChangeTracking
{
    public class RepositoryChangeTracking<TEntity, TChangeHistory>
    {
        public ISyncRepository<TEntity> Entities { get; private set; }
        public ISyncRepository<TChangeHistory> ChangeHistory { get; private set; }
        public bool Enabled { get; internal set; }

        public RepositoryChangeTracking(ISyncRepository<TEntity> entities, ISyncRepository<TChangeHistory> changeHistory)
        {
            this.Entities = entities;
            this.ChangeHistory = changeHistory;

            // Defaults.
            this.Enabled = true;
        }
    }
}
