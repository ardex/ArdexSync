﻿using System;
using System.Collections.Generic;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    /// <summary>
    /// Sync provider for entities with integrated versioning support.
    /// </summary>
    public class SimpleRepositorySyncProvider<TEntity, TVersion> : SimpleSyncProvider<TEntity, TVersion>
    {
        public Func<TEntity, TVersion> EntityVersionMapping { get; private set; }
        public SyncReplicaIdMapping<TEntity> OwnerReplicaIdMapping { get; private set; }

        private readonly IComparer<TVersion> __versionComparer;

        protected override IComparer<TVersion> VersionComparer
        {
            get { return __versionComparer; }
        }

        public SimpleRepositorySyncProvider(
            SyncReplicaInfo replicaInfo,
            SyncRepository<TEntity> repository,
            SyncGuidMapping<TEntity> entityGuidMapping,
            Func<TEntity, TVersion> entityVersionMapping,
            IComparer<TVersion> versionComparer,
            SyncReplicaIdMapping<TEntity> ownerReplicaIdMapping = null)
            : base(replicaInfo, repository, entityGuidMapping)
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
                var entityOwnerReplicaID = this.OwnerReplicaIdMapping == null ? 0 : this.OwnerReplicaIdMapping(entity);
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
            this.Repository.Lock.EnterReadLock();

            try
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
            finally
            {
                this.Repository.Lock.ExitReadLock();
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
