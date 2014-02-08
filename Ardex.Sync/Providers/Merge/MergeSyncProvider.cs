using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Ardex.Sync.ChangeTracking;

namespace Ardex.Sync.Providers.Merge
{
    public enum SyncConflictResolutionStrategy
    {
        Fail,
        Winner,
        Loser
    }

    public static class MergeSyncProvider
    {
        /// <summary>
        /// Factory method.
        /// </summary>
        public static MergeSyncProvider<TEntity, TChangeHistory> Create<TEntity, TChangeHistory>(
            ChangeTracking<TEntity, TChangeHistory> changeTracking)
        {
            return new MergeSyncProvider<TEntity, TChangeHistory>(changeTracking);
        }
    }

    /// <summary>
    /// Sync provider implementation which works with
    /// sync repositories and change history metadata.
    /// </summary>
    public class MergeSyncProvider<TEntity, TVersion> :
        MergeSyncProviderBase<TEntity, Dictionary<SyncID, TVersion>, TVersion>,
        ISyncMetadataCleanup<VersionInfo<TEntity, TVersion>>
    {
        /// <summary>
        /// Gets the change tracking manager used by this provider.
        /// </summary>
        public ChangeTracking<TEntity, TVersion> ChangeTracking { get; private set; }

        /// <summary>
        /// If true, the change history will be kept minimal
        /// by truncating all but the last entry at the end
        /// of the sync operation. Should only ever be enabled
        /// on the client in a client-server sync topology.
        /// The default is false.
        /// </summary>
        public bool CleanUpMetadataAfterSync { get; set; }

        protected override bool ChangeTrackingEnabled
        {
            get
            {
                return this.ChangeTracking.Enabled;
            }
            set
            {
                this.ChangeTracking.Enabled = value;
            }
        }

        protected override IComparer<TVersion> VersionComparer
        {
            get
            {
                return new ComparisonComparer<TVersion>(
                    (x, y) => this.ChangeTracking.GetChangeHistoryVersion(x).CompareTo(this.ChangeTracking.GetChangeHistoryVersion(y)));
            }
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public MergeSyncProvider(ChangeTracking<TEntity, TVersion> changeTracking) : base(changeTracking.ReplicaID, changeTracking.Repository, changeTracking.TrackedEntityIdMapping)
        {
            this.ChangeTracking = changeTracking;
        }

        /// <summary>
        /// Reports changes since the last reported version for each node.
        /// </summary>
        public override Delta<Dictionary<SyncID, TVersion>, VersionInfo<TEntity, TVersion>> ResolveDelta(Dictionary<SyncID, TVersion> versionByReplica, CancellationToken ct)
        {
            this.ChangeTracking.ChangeHistory.Lock.EnterReadLock();

            try
            {
                var anchor = this.LastAnchor();

                var changes = this.ChangeTracking
                    .FilteredChangeHistory()
                    .Where(ch =>
                    {
                        var version = default(TVersion);

                        return
                            !versionByReplica.TryGetValue(this.ChangeTracking.GetChangeHistoryReplicaID(ch), out version) ||
                            this.VersionComparer.Compare(ch, version) > 0;
                    })
                    .Join(
                        this.ChangeTracking.Repository.AsEnumerable(),
                        ch => this.ChangeTracking.GetChangeHistoryEntityID(ch),
                        this.ChangeTracking.GetTrackedEntityID,
                        (ch, entity) => VersionInfo.Create(entity, ch))
                    // Ensure that the oldest changes for each replica are sync first.
                    .OrderBy(c => this.ChangeTracking.GetChangeHistoryVersion(c.Version))
                    .AsEnumerable();

                return new Delta<Dictionary<SyncID, TVersion>, VersionInfo<TEntity, TVersion>>(anchor, changes);
            }
            finally
            {
                this.ChangeTracking.ChangeHistory.Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        public override Dictionary<SyncID, TVersion> LastAnchor()
        {
            return this.LastKnownVersionByReplica(this.ChangeTracking.FilteredChangeHistory());
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        private Dictionary<SyncID, TVersion> LastKnownVersionByReplica(IEnumerable<TVersion> changeHistory)
        {
            var dict = new Dictionary<SyncID, TVersion>();

            foreach (var ch in changeHistory)
            {
                var replicaID = this.ChangeTracking.GetChangeHistoryReplicaID(ch);
                var lastKnownVersion = default(TVersion);

                if (!dict.TryGetValue(replicaID, out lastKnownVersion) || this.VersionComparer.Compare(ch, lastKnownVersion) > 0)
                {
                    dict[replicaID] = ch;
                }
            }

            return dict;
        }

        /// <summary>
        /// Performs change history cleanup if necessary.
        /// Ensures that only the latest value for each node is kept.
        /// </summary>
        public void CleanUpSyncMetadata(IEnumerable<VersionInfo<TEntity, TVersion>> appliedDelta)
        {
            if (!this.CleanUpMetadataAfterSync)
                return;

            var changeHistory = this.ChangeTracking.ChangeHistory;

            // We need exclusive access to change
            // history during the cleanup operation.
            changeHistory.Lock.EnterWriteLock();

            try
            {
                var lastKnownVersionByReplica = this.LastKnownVersionByReplica(appliedDelta.Select(c => c.Version));

                foreach (var ch in this.ChangeTracking.FilteredChangeHistory())
                {
                    // Ensure that this change is not the last for node.
                    var replicaID = this.ChangeTracking.GetChangeHistoryReplicaID(ch);
                    var lastKnownVersion = default(TVersion);

                    if (lastKnownVersionByReplica.TryGetValue(replicaID, out lastKnownVersion) &&
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

        protected override void WriteRemoteVersion(VersionInfo<TEntity, TVersion> remoteVersion)
        {
            this.ChangeTracking.InsertChangeHistory(remoteVersion.Version);
        }
    }
}
