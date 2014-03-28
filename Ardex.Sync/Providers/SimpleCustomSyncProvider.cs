using System;
using System.Collections.Generic;

namespace Ardex.Sync.Providers
{
    public class SimpleCustomSyncProvider<TEntity, TKey, TVersion> : SimpleSyncProvider<TEntity, TKey, TVersion>
        where TEntity : class
    {
        private readonly Func<SyncAnchor<TVersion>> __lastAnchorFunc;
        private readonly Func<SyncAnchor<TVersion>, SyncDelta<TEntity, TVersion>> __resolveDeltaFunc;

        protected override IComparer<TVersion> VersionComparer
        {
            get { throw new NotSupportedException(); }
        }

        public SimpleCustomSyncProvider(
            SyncReplicaInfo replicaInfo,
            Func<SyncAnchor<TVersion>> lastAnchorFunc,
            Func<SyncAnchor<TVersion>, SyncDelta<TEntity, TVersion>> resolveDeltaFunc) : base(replicaInfo, null)
        {
            __lastAnchorFunc = lastAnchorFunc;
            __resolveDeltaFunc = resolveDeltaFunc;
        }

        public override SyncAnchor<TVersion> LastAnchor()
        {
            return __lastAnchorFunc();
        }

        public override SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor)
        {
            return __resolveDeltaFunc(remoteAnchor);
        }
    }
}
