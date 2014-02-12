using System;
using System.Collections.Generic;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public abstract class ChangeHistorySyncProvider<TEntity, TChangeHistory> :
        SyncProvider<TEntity, TChangeHistory>
        where TChangeHistory : IChangeHistory
    {
        public SyncRepository<TChangeHistory> ChangeHistory { get; private set; }

        protected override IComparer<TChangeHistory> VersionComparer
        {
            get
            {
                return new CustomComparer<TChangeHistory>(
                    (x, y) => x.Timestamp.CompareTo(y.Timestamp));
            }
        }

        protected abstract IEnumerable<TChangeHistory> FilteredChangeHistory { get; }

        public ChangeHistorySyncProvider(
            SyncReplicaInfo replicaInfo,
            SyncRepository<TEntity> repository,
            SyncRepository<TChangeHistory> changeHistory,
            SyncGuidMapping<TEntity> entityGuidMapping) : base(replicaInfo, repository, entityGuidMapping)
        {
            this.ChangeHistory = changeHistory;

            // Set up change tracking.
            this.Repository.TrackedChange += this.HandleTrackedChange;
        }

        protected abstract TChangeHistory CreateChangeHistoryForLocalChange(TEntity entity, SyncEntityChangeAction action);
        protected abstract TChangeHistory CreateChangeHistoryForRemoteChange(SyncEntityVersion<TEntity, TChangeHistory> remoteVersionInfo);
    
        private void HandleTrackedChange(TEntity entity, SyncEntityChangeAction action)
        {
            this.ChangeHistory.Lock.EnterWriteLock();

            try
            {
                var ch = this.CreateChangeHistoryForLocalChange(entity, action);

                this.ChangeHistory.Insert(ch);
            }
            finally
            {
                this.ChangeHistory.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// When overridden in a derived class, applies the
        /// given remote change entry locally if necessary.
        /// </summary>
        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, TChangeHistory> versionInfo)
        {
            this.ChangeHistory.Lock.EnterWriteLock();

            try
            {
                var ch = this.CreateChangeHistoryForRemoteChange(versionInfo);

                this.ChangeHistory.Insert(ch);
            }
            finally
            {
                this.ChangeHistory.Lock.ExitWriteLock();
            }
        }

        public override SyncAnchor<TChangeHistory> LastAnchor()
        {
            return this.LastKnownVersionByReplica(this.FilteredChangeHistory);
        }

        public override SyncDelta<TEntity, TChangeHistory> ResolveDelta(SyncAnchor<TChangeHistory> remoteAnchor)
        {
            // We need locks on both repositories.
            this.Repository.Lock.EnterReadLock();

            try
            {
                this.ChangeHistory.Lock.EnterReadLock();

                try
                {
                    var myAnchor = this.LastAnchor();

                    var myChanges = this.FilteredChangeHistory
                        .Where(ch =>
                        {
                            var version = default(TChangeHistory);

                            return
                                !remoteAnchor.TryGetValue(ch.ReplicaID, out version) ||
                                this.VersionComparer.Compare(ch, version) > 0;
                        })
                        .Join(
                            this.Repository.AsEnumerable(),
                            ch => ch.EntityGuid,
                            this.EntityGuidMapping.Get,
                            (ch, entity) => SyncEntityVersion.Create(entity, ch))
                        // Ensure that the oldest changes for each replica are sync first.
                        .OrderBy(c => c.Version, this.VersionComparer)
                        .AsEnumerable();

                    return SyncDelta.Create(this.ReplicaInfo, myAnchor, myChanges);
                }
                finally
                {
                    this.ChangeHistory.Lock.ExitReadLock();
                }
            }
            finally
            {
                this.Repository.Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Performs change history cleanup if necessary.
        /// Ensures that only the latest value for each node is kept.
        /// </summary>
        protected override void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, TChangeHistory>> appliedDelta)
        {
            // We need exclusive access to change
            // history during the cleanup operation.
            this.ChangeHistory.Lock.EnterWriteLock();

            try
            {
                var lastKnownVersionByReplica = this.LastKnownVersionByReplica(appliedDelta.Select(v => v.Version));

                foreach (var ch in this.FilteredChangeHistory)
                {
                    // Ensure that this change is not the last for node.
                    var lastKnownVersion = default(TChangeHistory);

                    if (lastKnownVersionByReplica.TryGetValue(ch.ReplicaID, out lastKnownVersion) &&
                        this.VersionComparer.Compare(ch, lastKnownVersion) < 0)
                    {
                        this.ChangeHistory.Delete(ch);
                    }
                }
            }
            finally
            {
                this.ChangeHistory.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        protected SyncAnchor<TChangeHistory> LastKnownVersionByReplica(IEnumerable<TChangeHistory> changeHistory)
        {
            var dict = new SyncAnchor<TChangeHistory>();

            foreach (var ch in changeHistory)
            {
                var lastKnownVersion = default(TChangeHistory);

                if (!dict.TryGetValue(ch.ReplicaID, out lastKnownVersion) ||
                    this.VersionComparer.Compare(ch, lastKnownVersion) > 0)
                {
                    dict[ch.ReplicaID] = ch;
                }
            }

            return dict;
        }

        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Unhook events to help the GC do its job.
            this.Repository.TrackedChange -= this.HandleTrackedChange;

            // Release refs.
            this.ChangeHistory = null;

            base.Dispose(disposing);

            _disposed = true;
        }
    }
}
