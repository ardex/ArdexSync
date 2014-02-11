using System;
using System.Collections.Generic;

namespace Ardex.Sync.Providers
{
    public class SimpleCustomSyncProvider<TEntity, TVersion> : SimpleSyncProvider<TEntity, TVersion>
    {
        private readonly Func<SyncAnchor<TVersion>> __lastAnchorFunc;
        private readonly Func<SyncAnchor<TVersion>, IEnumerable<SyncEntityVersion<TEntity, TVersion>>> __resolveChangesFunc;

        protected override IComparer<TVersion> VersionComparer
        {
            get { throw new NotImplementedException(); }
        }

        public SimpleCustomSyncProvider(
            int replicaID,
            Func<SyncAnchor<TVersion>> lastAnchorFunc,
            Func<SyncAnchor<TVersion>, IEnumerable<SyncEntityVersion<TEntity, TVersion>>> resolveChangesFunc) : base(replicaID, null, null)
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

            return SyncDelta.Create(this.ReplicaID, myAnchor, myChanges);
        }
    }
}
