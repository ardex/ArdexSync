using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public class ChangeHistorySyncProvider<TEntity, TChangeHistory> : SyncProvider<TEntity, Guid, TChangeHistory>
        where TEntity : class
        where TChangeHistory : IChangeHistory, new()
    {
        /// <summary>
        /// Gets the change history repository associated with this provider.
        /// </summary>
        public ISyncRepository<TChangeHistory> ChangeHistory { get; private set; }

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
            ISyncRepository<TEntity> repository,
            ISyncRepository<TChangeHistory> changeHistory,
            SyncEntityKeyMapping<TEntity, Guid> entityKeyMapping) 
            : base(replicaInfo, repository, entityKeyMapping)
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
                // Resolve pk and version.
                var changeHistoryID = 0;
                var timestamp = new Timestamp(0);

                foreach (var c in this.ChangeHistory)
                {
                    if (c.ChangeHistoryID > changeHistoryID)
                        changeHistoryID = c.ChangeHistoryID;

                    if (c.Timestamp != null &&
                        c.Timestamp.CompareTo(timestamp) > 0)
                    {
                        timestamp = c.Timestamp;
                    }
                }

                var ch =
                    this.CustomChangeHistoryFactory != null ?
                    this.CustomChangeHistoryFactory() :
                    new TChangeHistory();

                ch.ChangeHistoryID = changeHistoryID + 1;
                ch.Action = action;
                ch.ArticleID = this.ArticleID;
                ch.ReplicaID = this.ReplicaInfo.ReplicaID;
                ch.EntityGuid = this.EntityKeyMapping(entity);
                ch.Timestamp = ++timestamp;

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
        }

        public override SyncAnchor<TChangeHistory> LastAnchor()
        {
            return this.LastKnownVersionByReplica(this.FilteredChangeHistory);
        }

        public override SyncDelta<TEntity, TChangeHistory> ResolveDelta(SyncAnchor<TChangeHistory> remoteAnchor)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                // We need locks on both repositories,
                // but we don't want to hold it for too long.
                var changeHistory = default(IReadOnlyList<TChangeHistory>);
                var entities = default(IReadOnlyList<TEntity>);
                var myAnchor = default(SyncAnchor<TChangeHistory>);

                using (this.Repository.SyncLock.ReadLock())
                using (this.ChangeHistory.SyncLock.ReadLock())
                {
                    changeHistory = this.FilteredChangeHistory
                        .Where(ch =>
                        {
                            var version = default(TChangeHistory);

                            return
                                !remoteAnchor.TryGetValue(ch.ReplicaID, out version) ||
                                this.VersionComparer.Compare(ch, version) > 0;
                        })
                        .ToList();

                    entities = this.Repository.ToList();
                    myAnchor = this.LastAnchor();
                }

                // Now that the locks are released,
                // it's time for some heavy lifting.
                var myChanges = changeHistory
                    .Join(
                        entities,
                        ch => ch.EntityGuid,
                        ch => this.EntityKeyMapping(ch),
                        (ch, entity) => SyncEntityVersion.Create(entity, ch))
                    // Ensure that the oldest changes for each replica are synchronised first.
                    .OrderBy(c => c.Version, this.VersionComparer)
                    .ToList();

                return SyncDelta.Create(this.ReplicaInfo, myAnchor, myChanges);

                //// We need locks on both repositories.
                //using (this.Repository.ReadLock())
                //using (this.ChangeHistory.ReadLock())
                //{
                //    var myAnchor = this.LastAnchor();

                //    var myChanges = this.FilteredChangeHistory
                //        .Where(ch =>
                //        {
                //            var version = default(TChangeHistory);

                //            return
                //                !remoteAnchor.TryGetValue(ch.ReplicaID, out version) ||
                //                this.VersionComparer.Compare(ch, version) > 0;
                //        })
                //        .ToList()
                //        .Join(
                //            this.Repository.ToList(),
                //            ch => ch.EntityGuid,
                //            ch => this.EntityKeyMapping(ch),
                //            (ch, entity) => SyncEntityVersion.Create(entity, ch))
                //        // Ensure that the oldest changes for each replica are sync first.
                //        .OrderBy(c => c.Version, this.VersionComparer)
                //        .AsEnumerable();

                //    return SyncDelta.Create(this.ReplicaInfo, myAnchor, myChanges);
                //}
            }
            finally
            {
                Debug.WriteLine("{0}.ResolveDelta() completed in {1:0.###} seconds.", this.GetType().Name, (float)sw.ElapsedMilliseconds / 1000);
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
