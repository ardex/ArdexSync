using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.ChangeTracking
{
    public class ChangeTrackingRegistration<TEntity, TVersion>
    {
        private readonly SyncRepository<TVersion> __changeHistory;

        /// <summary>
        /// Gets the unique ID of this replica.
        /// </summary>
        public SyncID ReplicaID { get; private set; }

        /// <summary>
        /// Gets the tracked repository.
        /// </summary>
        public SyncRepository<TEntity> Repository { get; private set; }

        /// <summary>
        /// Gets the change history repository which contains
        /// the change metadata for the tracked repository.
        /// </summary>
        public SyncRepository<TVersion> ChangeHistory
        {
            get { return __changeHistory; }
        }

        private readonly Func<TVersion, bool> ChangeHistoryPredicate;

        // Essential member mapping.
        public readonly UniqueIdMapping<TEntity> TrackedEntityIdMapping;
        private readonly UniqueIdMapping<TVersion> ChangeHistoryIdMapping;
        private readonly UniqueIdMapping<TVersion> ChangeHistoryEntityIdMapping;
        private readonly UniqueIdMapping<TVersion> ChangeHistoryReplicaIdMapping;

        public IComparer<TVersion> VersionComparer { get; private set; }

        /// <summary>
        /// Indicates whether change tracking is turned on.
        /// </summary>
        public bool Enabled { get; internal set; }

        // Tracked/untracked change actions.
        // Note that it's the repository's responsibility
        // to actually insert/update/delete entities.
        // All we need to do is create change history entries to reflect the change.
        public event Action<TEntity, ChangeHistoryAction> TrackedChange;
        public event Action<TVersion> DirectInsertRequest;

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public ChangeTrackingRegistration(
            SyncID replicaID,
            SyncRepository<TEntity> entities,
            SyncRepository<TVersion> changeHistory,
            UniqueIdMapping<TEntity> trackedEntityIdMapping,
            Func<TVersion, bool> changeHistoryPredicate,
            UniqueIdMapping<TVersion> changeHistoryIdMapping,
            UniqueIdMapping<TVersion> changeHistoryEntityIdMapping,
            UniqueIdMapping<TVersion> changeHistoryReplicaIdMapping,
            IComparer<TVersion> versionComparer)
        {
            // Linked repositories.
            this.ReplicaID = replicaID;
            this.Repository = entities;

            __changeHistory = changeHistory;

            this.ChangeHistoryPredicate = changeHistoryPredicate;

            // Essential member mapping.
            this.TrackedEntityIdMapping = trackedEntityIdMapping;
            this.ChangeHistoryIdMapping = changeHistoryIdMapping;
            this.ChangeHistoryEntityIdMapping = changeHistoryEntityIdMapping;
            this.ChangeHistoryReplicaIdMapping = changeHistoryReplicaIdMapping;
            this.VersionComparer = versionComparer;

            // Defaults.
            this.Enabled = true;

            // Events.
            this.Repository.EntityInserted += e => this.OnTrackedChange(e, ChangeHistoryAction.Insert);
            this.Repository.EntityUpdated += e => this.OnTrackedChange(e, ChangeHistoryAction.Update);
            this.Repository.EntityDeleted += e => this.OnTrackedChange(e, ChangeHistoryAction.Delete);
        }

        protected virtual void OnTrackedChange(TEntity entity, ChangeHistoryAction action)
        {
            if (this.TrackedChange != null)
            {
                if (!this.Enabled)
                    return;

                __changeHistory.Lock.EnterWriteLock();

                try
                {
                    this.TrackedChange(entity, action);
                }
                finally
                {
                    __changeHistory.Lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Returns the the change history collection filtered according
        /// to the predicate specified when this instance was created.
        /// </summary>
        public IEnumerable<TVersion> FilteredChangeHistory()
        {
            return __changeHistory.Where(this.ChangeHistoryPredicate);
        }

        /// <summary>
        /// Creates local change history entry mirroring
        /// the change history entry which is returned by
        /// a remote replica during change-based sync.
        /// </summary>
        public void InsertChangeHistory(TVersion changeHistoryEntry)
        {
            if (this.DirectInsertRequest != null)
            {
                __changeHistory.Lock.EnterWriteLock();

                try
                {
                    this.DirectInsertRequest(changeHistoryEntry);
                }
                finally
                {
                    __changeHistory.Lock.ExitWriteLock();
                }
            }
        }

        public SyncID GetTrackedEntityID(TEntity entity)
        {
            return this.TrackedEntityIdMapping.Get(entity);
        }

        public SyncID GetChangeHistoryID(TVersion changeHistory)
        {
            return this.ChangeHistoryIdMapping.Get(changeHistory);
        }

        public SyncID GetChangeHistoryEntityID(TVersion changeHistory)
        {
            return this.ChangeHistoryEntityIdMapping.Get(changeHistory);
        }

        public SyncID GetChangeHistoryReplicaID(TVersion changeHistory)
        {
            return this.ChangeHistoryReplicaIdMapping.Get(changeHistory);
        }

        /// <summary>
        /// Reports changes since the last reported version for each node.
        /// </summary>
        public SyncDelta<TEntity, TVersion> ResolveDelta(Dictionary<SyncID, TVersion> versionByReplica, CancellationToken ct)
        {
            this.ChangeHistory.Lock.EnterReadLock();

            try
            {
                var anchor = this.LastAnchor();

                var changes = this
                    .FilteredChangeHistory()
                    .Where(ch =>
                    {
                        var version = default(TVersion);

                        return
                            !versionByReplica.TryGetValue(this.GetChangeHistoryReplicaID(ch), out version) ||
                            this.VersionComparer.Compare(ch, version) > 0;
                    })
                    .Join(
                        this.Repository.AsEnumerable(),
                        ch => this.GetChangeHistoryEntityID(ch),
                        this.GetTrackedEntityID,
                        (ch, entity) => SyncEntityVersion.Create(entity, ch))
                    // Ensure that the oldest changes for each replica are sync first.
                    .OrderBy(c => c.Version, this.VersionComparer)
                    .AsEnumerable();

                return SyncDelta.Create(anchor, changes);
            }
            finally
            {
                this.ChangeHistory.Lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        public Dictionary<SyncID, TVersion> LastAnchor()
        {
            return this.LastKnownVersionByReplica(this.FilteredChangeHistory());
        }

        /// <summary>
        /// Returns last seen version value for each known node.
        /// </summary>
        public Dictionary<SyncID, TVersion> LastKnownVersionByReplica(IEnumerable<TVersion> changeHistory)
        {
            var dict = new Dictionary<SyncID, TVersion>();

            foreach (var ch in changeHistory)
            {
                var replicaID = this.GetChangeHistoryReplicaID(ch);
                var lastKnownVersion = default(TVersion);

                if (!dict.TryGetValue(replicaID, out lastKnownVersion) ||
                    this.VersionComparer.Compare(ch, lastKnownVersion) > 0)
                {
                    dict[replicaID] = ch;
                }
            }

            return dict;
        }
    }
}
