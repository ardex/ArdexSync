using System;
using System.Collections.Generic;

using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Sync provider for entities with integrated versioning support.
    /// </summary>
    public class SimpleRepositorySyncProvider<TEntity, TKey, TVersion> : SimpleSyncProvider<TEntity, TKey, TVersion>
    {
        public SyncEntityVersionMapping<TEntity, TVersion> EntityVersionMapping { get; private set; }
        public SyncEntityOwnerMapping<TEntity> OwnerReplicaIdMapping { get; private set; }

        private readonly IComparer<TVersion> __versionComparer;

        protected override IComparer<TVersion> VersionComparer
        {
            get { return __versionComparer; }
        }

        public SimpleRepositorySyncProvider(
            SyncReplicaInfo replicaInfo,
            ISyncRepository<TEntity> repository,
            SyncEntityKeyMapping<TEntity, TKey> entityKeyMapping,
            SyncEntityVersionMapping<TEntity, TVersion> entityVersionMapping,
            IComparer<TVersion> versionComparer,
            SyncEntityOwnerMapping<TEntity> ownerReplicaIdMapping = null)
            : base(replicaInfo, repository, entityKeyMapping)
        {
            this.EntityVersionMapping = entityVersionMapping;
            this.OwnerReplicaIdMapping = ownerReplicaIdMapping;

            __versionComparer = versionComparer;
        }

        public override SyncAnchor<TVersion> LastAnchor()
        {
            var anchor = new SyncAnchor<TVersion>();

            // Lock taken by SyncRepository.GetEnumerator().
            foreach (var entity in this.Repository)
            {
                var entityOwnerReplicaID = this.GetOwnerReplicaID(entity);
                var entityVersion = this.EntityVersionMapping(entity);
                var maxVersion = default(TVersion);

                if (!anchor.TryGetValue(entityOwnerReplicaID, out maxVersion) ||
                    this.VersionComparer.Compare(entityVersion, maxVersion) > 0)
                {
                    anchor[entityOwnerReplicaID] = entityVersion;
                }
            }

            return anchor;
        }

        public override SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor)
        {
            using (this.Repository.ReadLock())
            {
                var myAnchor = this.LastAnchor();
                var myChanges = new List<SyncEntityVersion<TEntity, TVersion>>();

                foreach (var entity in this.Repository)
                {
                    var entityOwnerReplicaID = this.GetOwnerReplicaID(entity);
                    var entityVersion = this.EntityVersionMapping(entity);
                    var maxVersion = default(TVersion);

                    if (!remoteAnchor.TryGetValue(entityOwnerReplicaID, out maxVersion) ||
                        this.VersionComparer.Compare(entityVersion, maxVersion) > 0)
                    {
                        myChanges.Add(SyncEntityVersion.Create(entity, entityVersion));
                    }
                }

                return SyncDelta.Create(this.ReplicaInfo, myAnchor, myChanges);
            }
        }

        private int GetOwnerReplicaID(TEntity entity)
        {
            // Owner replica ID mapping is optional.
            if (this.OwnerReplicaIdMapping == null)
            {
                return 0;
            }

            return this.OwnerReplicaIdMapping(entity);
        }
    }
}
