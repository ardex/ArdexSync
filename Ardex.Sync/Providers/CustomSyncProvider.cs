using System;

namespace Ardex.Sync.Providers
{
    public class CustomSyncProvider<TEntity, TVersion> : ISyncProvider<TEntity, TVersion>
    {
        public SyncReplicaInfo ReplicaInfo { get; private set; }

        public Func<SyncAnchor<TVersion>> LastAnchorFunc { get; set; }
        public Func<SyncAnchor<TVersion>, SyncDelta<TEntity, TVersion>> ResolveDeltaFunc { get; set; }
        public Func<SyncDelta<TEntity, TVersion>, SyncResult> AcceptChangesFunc { get; set; }

        public CustomSyncProvider(SyncReplicaInfo replicaInfo)
        {
            this.ReplicaInfo = replicaInfo;
        }

        public SyncAnchor<TVersion> LastAnchor()
        {
            if (this.LastAnchorFunc != null)
            {
                return this.LastAnchorFunc();
            }

            throw new NotSupportedException();
        }

        public SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor)
        {
            if (this.ResolveDeltaFunc != null)
            {
                return this.ResolveDeltaFunc(remoteAnchor);
            }

            throw new NotSupportedException();
        }

        public SyncResult AcceptChanges(SyncDelta<TEntity, TVersion> remoteDelta)
        {
            if (this.AcceptChangesFunc != null)
            {
                return this.AcceptChangesFunc(remoteDelta);
            }

            throw new NotSupportedException();
        }
    }
}

