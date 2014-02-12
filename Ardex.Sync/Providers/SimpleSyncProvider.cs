using System;
using System.Collections.Generic;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public abstract class SimpleSyncProvider<TEntity, TVersion> : SyncProvider<TEntity, TVersion>
    {
        public override SyncConflictStrategy ConflictStrategy
        {
            get { return SyncConflictStrategy.Fail; }
            set { throw new NotSupportedException("Simple sync providers do not support conflict resolution."); }
        }

        public override bool CleanUpMetadata
        {
            get { return false; }
            set { throw new NotSupportedException("Simple sync providers do not support metadata cleanup."); }
        }

        public SimpleSyncProvider(
            SyncReplicaInfo replicaInfo,
            SyncRepository<TEntity> repository,
            SyncGuidMapping<TEntity> entityGuidMapping) : base(replicaInfo, repository, entityGuidMapping)
        {
        
        }

        protected override void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, TVersion>> appliedChanges)
        {
            throw new NotSupportedException("Simple sync providers do not support metadata cleanup.");
        }

        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, TVersion> versionInfo)
        {
            // We do not really need to do this because timestamp
            // and ownership info will be updated by ApplyChanges.
        }
    }
}
