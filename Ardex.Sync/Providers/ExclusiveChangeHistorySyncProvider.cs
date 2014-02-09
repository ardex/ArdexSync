using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.Providers
{
    public class ExclusiveChangeHistorySyncProvider<TEntity> : ChangeHistorySyncProvider<TEntity, IChangeHistory>
    {
        public ExclusiveChangeHistorySyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> entityIdMapping) : base(replicaID, repository, changeHistory, entityIdMapping)
        {

        }
    }
}
