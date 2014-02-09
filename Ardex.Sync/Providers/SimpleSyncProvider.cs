using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ardex.Sync.Providers
{
    public abstract class SimpleSyncProvider<TEntity, TVersion> : SyncProvider<TEntity, TVersion>
    {
        public SimpleSyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            UniqueIdMapping<TEntity> entityIdMapping) : base(replicaID, repository, entityIdMapping)
        {
        
        }

        protected override void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, TVersion>> appliedChanges)
        {
            throw new NotSupportedException();
        }

        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, TVersion> versionInfo)
        {
            // We do not really need to do this because timestamp
            // and ownership info will be updated by ApplyChanges.
        }
    }
}
