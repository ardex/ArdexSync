using System;
using System.Collections.Generic;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public class ChangeHistorySyncProvider<TEntity> : SyncProvider<TEntity, Guid, IChangeHistory>
    {
        /// <summary>
        /// Gets the change history repository associated with this provider.
        /// </summary>
        public SyncRepository<IChangeHistory> ChangeHistory { get; private set; }

        /// <summary>
        /// Gets or sets the unique article ID which is used to
        /// generate unique entity IDs and filter change history.
        /// </summary>
        public short ArticleID { get; set; }

        protected override IComparer<IChangeHistory> VersionComparer
        {
            get
            {
                return Comparer<IChangeHistory>.Create(
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

        public ChangeHistorySyncProvider(
            SyncReplicaInfo replicaInfo,
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistory,
            SyncEntityKeyMapping<TEntity, Guid> entityKeyMapping) : base(replicaInfo, repository, entityKeyMapping)
        {
            this.ChangeHistory = changeHistory;

            // Set up change tracking.
            this.Repository.TrackedChange += this.HandleTrackedChange;
        }
    
        private void HandleTrackedChange(TEntity entity, SyncEntityChangeAction action)
        {
            if (!this.ChangeHistory.Lock.TryEnterWriteLock(SyncConstants.DeadlockTimeout))
            {
                throw new SyncDeadlockException();
            }

            try
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

        #region ID generation

        private SyncGuid LastGeneratedGuid = null;

        public Guid NewSequentialID()
        {
            var guid = new SyncGuid(
                this.ReplicaInfo.ReplicaID,
                this.ArticleID,
                this.LastGeneratedGuid == null ? 1 : this.LastGeneratedGuid.EntityID + 1);

            this.LastGeneratedGuid = guid;

            return guid;
        }

        #endregion

        #region Cleanup

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

        #endregion
    }
}
