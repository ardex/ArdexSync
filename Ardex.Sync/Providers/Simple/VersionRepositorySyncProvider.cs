using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.Providers.Simple
{
    /// <summary>
    /// Version-based sync provider which
    /// uses a repository for producing
    /// and accepting change delta.
    /// </summary>
    public class VersionRepositorySyncProvider<TEntity> : SyncProvider<TEntity, IComparable, IComparable>
    {
        /// <summary>
        /// Provides means of getting a version from an entity.
        /// </summary>
        public ComparableMapping<TEntity> VersionMapping { get; private set; }

        /// <summary>
        /// Uniquely identifies the sole replica which is
        /// allowed to make changes to a particular entity.
        /// </summary>
        public UniqueIdMapping<TEntity> OwnerReplicaIdMapping { get; set; }

        protected override IComparer<IComparable> VersionComparer
        {
            get
            {
                return new ComparisonComparer<IComparable>((x, y) => x.CompareTo(y));
            }
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public VersionRepositorySyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            UniqueIdMapping<TEntity> uniqueIdMapping,
            ComparableMapping<TEntity> versionMapping) : base(replicaID, repository, uniqueIdMapping)
        {
            this.VersionMapping = versionMapping;
        }

        public override SyncDelta<TEntity, IComparable, IComparable> ResolveDelta(IComparable lastKnownVersion, CancellationToken ct)
        {
            this.Repository.Lock.EnterReadLock();

            try
            {
                var ownerReplicaIdMapping = this.OwnerReplicaIdMapping;
                var anchor = this.LastAnchor();

                var changes = this.Repository
                    .Where(e =>
                        (ownerReplicaIdMapping == null || this.OwnerReplicaIdMapping.Get(e) == this.ReplicaID) &&
                        (lastKnownVersion == null || this.VersionMapping.Get(e).CompareTo(lastKnownVersion) > 0))
                    .OrderBy(e => this.VersionMapping.Get(e))
                    .Select(e => SyncEntityVersion.Create(e, this.VersionMapping.Get(e)));

                return SyncDelta.Create(anchor, changes);
            }
            finally
            {
                this.Repository.Lock.ExitReadLock();
            }
        }

        public override IComparable LastAnchor()
        {
            return this.Repository
                .Select(e => this.VersionMapping.Get(e))
                .DefaultIfEmpty()
                .Max();
        }
    }
}
