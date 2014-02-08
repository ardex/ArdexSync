using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Ardex.Sync.ChangeTracking;

namespace Ardex.Sync.Providers.Merge
{
    public static class MergeSyncProvider
    {
        /// <summary>
        /// Factory method.
        /// </summary>
        public static MergeSyncProvider<TEntity, TChangeHistory> Create<TEntity, TChangeHistory>(
            ChangeTrackingRegistration<TEntity, TChangeHistory> changeTracking)
        {
            return new MergeSyncProvider<TEntity, TChangeHistory>(changeTracking);
        }
    }

    /// <summary>
    /// Sync provider implementation which works with
    /// sync repositories and change history metadata.
    /// </summary>
    public class MergeSyncProvider<TEntity, TVersion> : SyncProvider<TEntity, TVersion>
    {
        /// <summary>
        /// Gets the change tracking manager used by this provider.
        /// </summary>
        public ChangeTrackingRegistration<TEntity, TVersion> ChangeTracking { get; private set; }

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
                return this.ChangeTracking.VersionComparer;
            }
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public MergeSyncProvider(ChangeTrackingRegistration<TEntity, TVersion> changeTracking)
            : base(changeTracking.ReplicaID, changeTracking.Repository, changeTracking.TrackedEntityIdMapping)
        {
            this.ChangeTracking = changeTracking;
        }

        /// <summary>
        /// Reports changes since the last reported version for each node.
        /// </summary>
        public override SyncDelta<TEntity, TVersion> ResolveDelta(Dictionary<SyncID, TVersion> versionByReplica, CancellationToken ct)
        {
            return this.ChangeTracking.ResolveDelta(versionByReplica, ct);
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        public override Dictionary<SyncID, TVersion> LastAnchor()
        {
            return this.ChangeTracking.LastAnchor();
        }

        /// <summary>
        /// Performs change history cleanup if necessary.
        /// Ensures that only the latest value for each node is kept.
        /// </summary>
        protected override void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, TVersion>> appliedDelta)
        {
            if (!this.CleanUpMetadataAfterSync)
                return;

            var changeHistory = this.ChangeTracking.ChangeHistory;

            // We need exclusive access to change
            // history during the cleanup operation.
            changeHistory.Lock.EnterWriteLock();

            try
            {
                var lastKnownVersionByReplica = this.ChangeTracking.LastKnownVersionByReplica(appliedDelta.Select(c => c.Version));

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

        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, TVersion> remoteVersion)
        {
            this.ChangeTracking.InsertChangeHistory(remoteVersion.Version);
        }
    }
}
