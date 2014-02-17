using System;
using System.Collections.Generic;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public class ChangeHistorySyncProvider<TEntity> : SyncProvider<TEntity, Guid, IChangeHistory>
    {
        private bool DisposeRepositories { get; set; }

        public SyncRepository<IChangeHistory> ChangeHistory { get; private set; }

        protected override IComparer<IChangeHistory> VersionComparer
        {
            get
            {
                return new CustomComparer<IChangeHistory>(
                    (x, y) => x.Timestamp.CompareTo(y.Timestamp)
                );
            }
        }

        protected virtual IEnumerable<IChangeHistory> FilteredChangeHistory
        {
            get
            {
                if (this.ArticleID == 0)
                {
                    return this.ChangeHistory;
                }

                return this.ChangeHistory.Where(ch => ch.ArticleID == this.ArticleID);
            }
        }

        public short ArticleID { get; set; }

        public ChangeHistorySyncProvider(
            SyncReplicaInfo replicaInfo,
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistory,
            SyncEntityKeyMapping<TEntity, Guid> entityKeyMapping) : base(replicaInfo, repository, entityKeyMapping)
        {
            this.ChangeHistory = changeHistory;
            this.DisposeRepositories = false;

            // Set up change tracking.
            this.Repository.TrackedChange += this.HandleTrackedChange;
        }

        protected virtual IChangeHistory CreateChangeHistoryForLocalChange(TEntity entity, SyncEntityChangeAction action)
        {
            var ch = (IChangeHistory)new ChangeHistory();

            // Resolve pk.
            ch.ChangeHistoryID = this.ChangeHistory
                .Select(c => c.ChangeHistoryID)
                .DefaultIfEmpty()
                .Max() + 1;

            ch.Action = action;
            ch.ArticleID = this.ArticleID;
            ch.ReplicaID = this.ReplicaInfo.ReplicaID;
            ch.EntityGuid = this.EntityKeyMapping(entity);

            // Resolve version.
            var timestamp = this.ChangeHistory
                .Where(c => c.ReplicaID == this.ReplicaInfo.ReplicaID)
                .Select(c => c.Timestamp)
                .DefaultIfEmpty()
                .Max();

            ch.Timestamp = (timestamp == null ? new Timestamp(1) : ++timestamp);

            return ch;
        }

        /// <summary>
        /// When overridden in a derived class, applies the
        /// given remote change entry locally if necessary.
        /// </summary>
        protected virtual IChangeHistory CreateChangeHistoryForRemoteChange(SyncEntityVersion<TEntity, IChangeHistory> versionInfo)
        {
            var ch = (IChangeHistory)new ChangeHistory();

            // Resolve pk.
            ch.ChangeHistoryID = this.ChangeHistory
                .Select(c => c.ChangeHistoryID)
                .DefaultIfEmpty()
                .Max() + 1;

            ch.Action = versionInfo.Version.Action;
            ch.ArticleID = this.ArticleID;
            ch.ReplicaID = versionInfo.Version.ReplicaID;
            ch.EntityGuid = versionInfo.Version.EntityGuid;
            ch.Timestamp = versionInfo.Version.Timestamp;

            return ch;
        }
    
        private void HandleTrackedChange(TEntity entity, SyncEntityChangeAction action)
        {
            if (!this.ChangeHistory.Lock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

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
        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, IChangeHistory> versionInfo)
        {
            if (!this.ChangeHistory.Lock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

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

        public override SyncAnchor<IChangeHistory> LastAnchor()
        {
            return this.LastKnownVersionByReplica(this.FilteredChangeHistory);
        }

        public override SyncDelta<TEntity, IChangeHistory> ResolveDelta(SyncAnchor<IChangeHistory> remoteAnchor)
        {
            // We need locks on both repositories.
            if (!this.Repository.Lock.TryEnterReadLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

            try
            {
                if (!this.ChangeHistory.Lock.TryEnterReadLock(SyncConstants.DeadlockTimeout))
                {
                    throw new SyncDeadlockException();
                }

                try
                {
                    var myAnchor = this.LastAnchor();

                    var myChanges = this.FilteredChangeHistory
                        .Where(ch =>
                        {
                            var version = default(IChangeHistory);

                            return
                                !remoteAnchor.TryGetValue(ch.ReplicaID, out version) ||
                                this.VersionComparer.Compare(ch, version) > 0;
                        })
                        .Join(
                            this.Repository.AsEnumerable(),
                            ch => ch.EntityGuid,
                            ch => this.EntityKeyMapping(ch),
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
        protected override void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, IChangeHistory>> appliedDelta)
        {
            // We need exclusive access to change
            // history during the cleanup operation.
            if (!this.ChangeHistory.Lock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

            try
            {
                var lastKnownVersionByReplica = this.LastKnownVersionByReplica(appliedDelta.Select(v => v.Version));

                foreach (var ch in this.FilteredChangeHistory)
                {
                    // Ensure that this change is not the last for node.
                    var lastKnownVersion = default(IChangeHistory);

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
        protected SyncAnchor<IChangeHistory> LastKnownVersionByReplica(IEnumerable<IChangeHistory> changeHistory)
        {
            var dict = new SyncAnchor<IChangeHistory>();

            foreach (var ch in changeHistory)
            {
                var lastKnownVersion = default(IChangeHistory);

                if (!dict.TryGetValue(ch.ReplicaID, out lastKnownVersion) ||
                    this.VersionComparer.Compare(ch, lastKnownVersion) > 0)
                {
                    dict[ch.ReplicaID] = ch;
                }
            }

            return dict;
        }

        public Guid GenerateEntityGuid(long entityID)
        {
            return new SyncGuid(this.ReplicaInfo.ReplicaID, this.ArticleID, entityID);
        }

        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Unhook events to help the GC do its job.
            this.Repository.TrackedChange -= this.HandleTrackedChange;

            if (this.DisposeRepositories)
            {
                this.Repository.Dispose();
                this.ChangeHistory.Dispose();
            }

            // Release refs.
            this.ChangeHistory = null;

            base.Dispose(disposing);

            _disposed = true;
        }
    }
}
