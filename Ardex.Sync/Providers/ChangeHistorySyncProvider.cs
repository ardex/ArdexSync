using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.Providers
{
    public class ChangeHistorySyncProvider<TEntity> : SyncProvider<TEntity, IChangeHistory>
    {
        public SyncRepository<IChangeHistory> ChangeHistory { get; private set; }

        protected override IComparer<IChangeHistory> VersionComparer
        {
            get
            {
                return new ComparisonComparer<IChangeHistory>(
                    (x, y) => x.Timestamp.CompareTo(y.Timestamp));
            }
        }

        public ChangeHistorySyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> entityIdMapping) : base(replicaID, repository, entityIdMapping)
        {
            this.ChangeHistory = changeHistory;

            // Set up change tracking.
            this.Repository.EntityInserted += e => this.HandleRepositoryChange(e, ChangeHistoryAction.Insert);
            this.Repository.EntityUpdated += e => this.HandleRepositoryChange(e, ChangeHistoryAction.Update);
            this.Repository.EntityDeleted += e => this.HandleRepositoryChange(e, ChangeHistoryAction.Delete);
        }
    
        private void HandleRepositoryChange(TEntity entity, ChangeHistoryAction action)
        {
            if (this.ChangeTrackingEnabled)
            {
                this.ChangeHistory.Lock.EnterWriteLock();

                try
                {
                    var ch = (IChangeHistory)new ChangeHistory();

                    // Resolve pk.
                    ch.ChangeHistoryID = this.ChangeHistory
                        .Select(c => c.ChangeHistoryID)
                        .DefaultIfEmpty()
                        .Max() + 1;

                    ch.Action = action;
                    ch.ReplicaID = this.ReplicaID;
                    ch.UniqueID = this.EntityIdMapping.Get(entity);

                    // Resolve version.
                    var timestamp = this.ChangeHistory
                        .Where(c => c.ReplicaID == this.ReplicaID)
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
        }

        /// <summary>
        /// When overridden in a derived class, applies the
        /// given remote change entry locally if necessary.
        /// </summary>
        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, IChangeHistory> versionInfo)
        {
            this.ChangeHistory.Lock.EnterWriteLock();

            try
            {
                var ch = (IChangeHistory)new ChangeHistory();

                // Resolve pk.
                ch.ChangeHistoryID = this.ChangeHistory
                    .Select(c => c.ChangeHistoryID)
                    .DefaultIfEmpty()
                    .Max() + 1;

                ch.Action = versionInfo.Version.Action;
                ch.ReplicaID = versionInfo.Version.ReplicaID;
                ch.UniqueID = versionInfo.Version.UniqueID;
                ch.Timestamp = versionInfo.Version.Timestamp;

                this.ChangeHistory.Insert(ch);
            }
            finally
            {
                this.ChangeHistory.Lock.ExitWriteLock();
            }
        }

        public override Dictionary<SyncID, IChangeHistory> LastAnchor()
        {
            return this.LastKnownVersionByReplica(this.ChangeHistory);
        }

        public override SyncDelta<TEntity, IChangeHistory> ResolveDelta(Dictionary<SyncID, IChangeHistory> anchor, CancellationToken ct)
        {
            this.ChangeHistory.Lock.EnterReadLock();

            try
            {
                var myAnchor = this.LastAnchor();

                var changes = this.ChangeHistory
                    .Where(ch =>
                    {
                        var version = default(IChangeHistory);

                        return
                            !anchor.TryGetValue(ch.ReplicaID, out version) ||
                            this.VersionComparer.Compare(ch, version) > 0;
                    })
                    .Join(
                        this.Repository.AsEnumerable(),
                        ch => ch.UniqueID,
                        this.EntityIdMapping.Get,
                        (ch, entity) => SyncEntityVersion.Create(entity, ch))
                    // Ensure that the oldest changes for each replica are sync first.
                    .OrderBy(c => c.Version, this.VersionComparer)
                    .AsEnumerable();

                return SyncDelta.Create(myAnchor, changes);
            }
            finally
            {
                this.ChangeHistory.Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Performs change history cleanup if necessary.
        /// Ensures that only the latest value for each node is kept.
        /// </summary>
        protected override void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, IChangeHistory>> appliedDelta)
        {
            if (!this.CleanUpMetadataAfterSync)
                return;

            var changeHistory = this.ChangeHistory;

            // We need exclusive access to change
            // history during the cleanup operation.
            changeHistory.Lock.EnterWriteLock();

            try
            {
                var lastKnownVersionByReplica = this.LastKnownVersionByReplica(appliedDelta.Select(v => v.Version));

                foreach (var ch in this.ChangeHistory)
                {
                    // Ensure that this change is not the last for node.
                    var lastKnownVersion = default(IChangeHistory);

                    if (lastKnownVersionByReplica.TryGetValue(ch.ReplicaID, out lastKnownVersion) &&
                        this.VersionComparer.Compare(ch, lastKnownVersion) < 0)
                    {
                        changeHistory.Delete(ch);
                    }
                }
            }
            finally
            {
                changeHistory.Lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        protected Dictionary<SyncID, IChangeHistory> LastKnownVersionByReplica(IEnumerable<IChangeHistory> changeHistory)
        {
            var dict = new Dictionary<SyncID, IChangeHistory>();

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
    }
}
