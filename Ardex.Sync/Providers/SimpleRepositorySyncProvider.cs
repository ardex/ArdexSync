﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Sync provider for entities with integrated versioning support.
    /// </summary>
    public class SimpleRepositorySyncProvider<TEntity, TVersion> : SimpleSyncProvider<TEntity, TVersion>
    {
        public Func<TEntity, TVersion> EntityVersionMapping { get; private set; }
        public UniqueIdMapping<TEntity> OwnerReplicaIdMapping { get; private set; }

        private readonly IComparer<TVersion> __versionComparer;

        protected override IComparer<TVersion> VersionComparer
        {
            get { return __versionComparer; }
        }

        public SimpleRepositorySyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            UniqueIdMapping<TEntity> entityIdMapping,
            Func<TEntity, TVersion> entityVersionMapping,
            IComparer<TVersion> versionComparer,
            UniqueIdMapping<TEntity> ownerReplicaIdMapping = null)
            : base(replicaID, repository, entityIdMapping)
        {
            this.EntityVersionMapping = entityVersionMapping;
            this.OwnerReplicaIdMapping = ownerReplicaIdMapping;

            __versionComparer = versionComparer;
        }

        public override SyncAnchor<TVersion> LastAnchor()
        {
            return SyncAnchor.Create(this.Repository, this.OwnerReplicaIdMapping, this.EntityVersionMapping, this.VersionComparer);
        }

        public override SyncDelta<TEntity, TVersion> ResolveDelta(SyncAnchor<TVersion> remoteAnchor, CancellationToken ct)
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

            return SyncDelta.Create(this.ReplicaID, myAnchor, myChanges);
        }

        private SyncID GetOwnerReplicaID(TEntity entity)
        {
            // Owner replica ID mapping is optional.
            if (this.OwnerReplicaIdMapping == null)
            {
                return default(SyncID);
            }

            return this.OwnerReplicaIdMapping.Get(entity);
        }
    }
}
