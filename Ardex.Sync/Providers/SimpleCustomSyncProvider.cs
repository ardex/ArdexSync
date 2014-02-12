using System;
using System.Collections.Generic;

namespace Ardex.Sync.Providers
{
    public class SimpleCustomSyncProvider<TEntity, TKey, TVersion> : SimpleSyncProvider<TEntity, TKey, TVersion>
    {
        private readonly Func<SyncAnchor<TVersion>> __lastAnchorFunc;
        private readonly Func<SyncAnchor<TVersion>, IEnumerable<SyncEntityVersion<TEntity, TVersion>>> __resolveChangesFunc;

        protected override IComparer<TVersion> VersionComparer
        {
            get { throw new NotSupportedException(); }
        }

        public SimpleCustomSyncProvider(
            SyncReplicaInfo replicaInfo,
            Func<SyncAnchor<TVersion>> lastAnchorFunc,
            Func<SyncAnchor<TVersion>, IEnumerable<SyncEntityVersion<TEntity, TVersion>>> resolveChangesFunc) : base(replicaInfo, null, null)
        {
            __lastAnchorFunc = lastAnchorFunc;
            __resolveChangesFunc = resolveChangesFunc;
        }

        public override SyncAnchor<TVersion> LastAnchor()
        {
            return __lastAnchorFunc();
        }

        public override SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor)
        {
            var myAnchor = this.LastAnchor();
            var myChanges = __resolveChangesFunc(remoteAnchor);

            return SyncDelta.Create(this.ReplicaInfo, myAnchor, myChanges);
        }
    }
}
