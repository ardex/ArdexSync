#define PARALLEL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Ardex.Sync.ChangeTracking;

namespace Ardex.Sync.Providers
{
    public class ChangeHistorySyncProvider<TEntity, TChangeHistory> : SyncProvider<TEntity, Guid, TChangeHistory>
        where TEntity : class
        where TChangeHistory : IChangeHistory, new()
    {
        /// <summary>
        /// Gets the change history repository associated with this provider.
        /// </summary>
        public ISyncRepository<int, TChangeHistory> ChangeHistory { get; private set; }

        /// <summary>
        /// Gets or sets the unique article ID which is used to
        /// generate unique entity IDs and filter change history.
        /// </summary>
        public short ArticleID { get; set; }

        /// <summary>
        /// Gets or sets the factory function used to create new 
        /// instances of the concrete IChangeHistory implementations.
        /// </summary>
        public Func<TChangeHistory> CustomChangeHistoryFactory { get; set; }

        protected override IComparer<TChangeHistory> VersionComparer
        {
            get
            {
                return Comparer<TChangeHistory>.Create(
                    (x, y) => x.Timestamp.CompareTo(y.Timestamp)
                );
            }
        }

        protected virtual IEnumerable<TChangeHistory> FilteredChangeHistory
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
            ISyncRepository<Guid, TEntity> repository,
            ISyncRepository<int, TChangeHistory> changeHistory) 
            : base(replicaInfo, repository)
        {
            // Parameters.
            this.ChangeHistory = changeHistory;

            // Set up change tracking.
            this.Repository.TrackedChange += this.HandleTrackedChange;
        }
    
        private void HandleTrackedChange(TEntity entity, SyncEntityChangeAction action)
        {
            using (this.ChangeHistory.SyncLock.WriteLock())
            {
                var ch =
                    this.CustomChangeHistoryFactory != null ?
                    this.CustomChangeHistoryFactory() :
                    new TChangeHistory();

                ch.ChangeHistoryID = this.NextChangeHistoryID();
                ch.Action = action;
                ch.ArticleID = this.ArticleID;
                ch.ReplicaID = this.ReplicaInfo.ReplicaID;
                ch.EntityGuid = this.Repository.KeySelector(entity);
                ch.Timestamp = this.NextTimestamp();

                this.ChangeHistory.Insert(ch);
            }
        }

        /// <summary>
        /// When overridden in a derived class, applies the
        /// given remote change entry locally if necessary.
        /// </summary>
        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, TChangeHistory> versionInfo)
        {
            using (this.ChangeHistory.SyncLock.WriteLock())
            {
                var ch =
                    this.CustomChangeHistoryFactory != null ?
                    this.CustomChangeHistoryFactory() :
                    new TChangeHistory();

                // Resolve pk.
                ch.ChangeHistoryID = this.NextChangeHistoryID();

                ch.Action = versionInfo.Version.Action;
                ch.ArticleID = this.ArticleID;
                ch.ReplicaID = versionInfo.Version.ReplicaID;
                ch.EntityGuid = versionInfo.Version.EntityGuid;
                ch.Timestamp = versionInfo.Version.Timestamp;

                this.ChangeHistory.Insert(ch);
            }
        }

        protected virtual int NextChangeHistoryID()
        {
            return this.ChangeHistory
                .Select(c => c.ChangeHistoryID)
                .DefaultIfEmpty()
                .Max() + 1;
        }

        protected virtual Timestamp NextTimestamp()
        {
            var maxTimestamp = this.ChangeHistory
                .Select(c => c.Timestamp)
                .DefaultIfEmpty()
                .Max();

            return maxTimestamp == null ? new Timestamp(1) : ++maxTimestamp;
        }

        public override SyncAnchor<TChangeHistory> LastAnchor()
        {
            return this.LastKnownVersionByReplica(this.FilteredChangeHistory);
        }

        public /*sealed*/ override SyncDelta<TEntity, TChangeHistory> ResolveDelta(SyncAnchor<TChangeHistory> remoteAnchor)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                using (this.Repository.SyncLock.ReadLock())
                {
                    // We're not taking a lock on change history.
                    var filteredChangeHistory = this.FilteredChangeHistory.ToList();

#if PARALLEL
                    // Parallel, woo-hoo!
                    var resolveChangesTask = Task.Factory.StartNew(state =>
                    {
                        // Micro-optimisation.
                        var changeHistory = (IEnumerable<TChangeHistory>)state;
#else
                        var changeHistory = filteredChangeHistory;
#endif
                        var changes = new List<TChangeHistory>();

                        foreach (var ch in changeHistory)
                        {
                            TChangeHistory version;

                            if (!remoteAnchor.TryGetValue(ch.ReplicaID, out version) ||
                                this.VersionComparer.Compare(ch, version) > 0)
                            {
                                changes.Add(ch);
                            }
                        }

#if PARALLEL
                        return changes;
                    },
                    filteredChangeHistory);
#endif

                    // Realistically we should be calling LastAnchor().
                    // This optimisation is the reason this method has been sealed.
                    var myAnchor = this.LastKnownVersionByReplica(this.FilteredChangeHistory);

                    #if PARALLEL
                    var changesSinceRemoteAnchor = resolveChangesTask.Result;
                    #else
                    var changesSinceRemoteAnchor = changes;
                    #endif

                    var myChanges = this.Repository
                        .Join(changesSinceRemoteAnchor, c => c.EntityGuid, (entity, ch) => SyncEntityVersion.Create(entity, ch))
                        .OrderBy(c => c.Version, this.VersionComparer) // Ensure that the oldest changes for each replica are synchronised first.
                        .ToList();

                    return SyncDelta.Create(this.ReplicaInfo, myAnchor, myChanges);
                }
            }
            finally
            {
                Debug.WriteLine(
                    "{0}.ResolveDelta() completed in {1:0.###} seconds.",
                    string.Format("{0}<{1}, {2}>", this.GetType().Name.Replace("`2", ""), typeof(TEntity).Name, typeof(TChangeHistory).Name),
                    (float)sw.ElapsedMilliseconds / 1000);
            }
        }

        /// <summary>
        /// Performs change history cleanup if necessary.
        /// Ensures that only the latest value for each node is kept.
        /// </summary>
        protected override void CleanUpSyncMetadata(IEnumerable<SyncEntityVersion<TEntity, TChangeHistory>> appliedChanges)
        {
            // Materialise changes.
            var appliedDelta = appliedChanges.ToList();

            if (appliedDelta.Count == 0)
            {
                // Common case optimisation: avoid taking lock.
                return;
            }

            // We need exclusive access to change
            // history during the cleanup operation.
            using (this.ChangeHistory.SyncLock.WriteLock())
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
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        protected SyncAnchor<TChangeHistory> LastKnownVersionByReplica(IEnumerable<TChangeHistory> changeHistory)
        {
            var dict = new SyncAnchor<TChangeHistory>(this.ReplicaInfo);

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

        #region Cleanup

        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Unhook events to help the GC do its job.
                this.Repository.TrackedChange -= this.HandleTrackedChange;

                // Release refs.
                this.ChangeHistory = null;
            }

            base.Dispose(disposing);

            _disposed = true;
        }

        #endregion
    }
}