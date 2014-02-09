using System.Collections.Generic;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;

namespace Ardex.Sync.Providers
{
    public class SharedChangeHistorySyncProvider<TEntity> : ChangeHistorySyncProvider<TEntity, ISharedChangeHistory>
    {
        public SyncID ArticleID { get; private set; }

        protected override IEnumerable<ISharedChangeHistory> FilteredChangeHistory
        {
            get
            {
                return base.FilteredChangeHistory.Where(ch => ch.ArticleID == this.ArticleID);
            }
        }

        public SharedChangeHistorySyncProvider(
            SyncID replicaID,
            SyncID articleID,
            SyncRepository<TEntity> repository,
            SyncRepository<ISharedChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> entityIdMapping) : base(replicaID, repository, changeHistory, entityIdMapping)
        {
            this.ArticleID = articleID;
        }

        internal override void HandleRepositoryChange(TEntity entity, ChangeHistoryAction action)
        {
            if (this.ChangeTrackingEnabled)
            {
                this.ChangeHistory.Lock.EnterWriteLock();

                try
                {
                    var ch = (ISharedChangeHistory)new SharedChangeHistory();

                    // Resolve pk.
                    ch.ChangeHistoryID = this.ChangeHistory
                        .Select(c => c.ChangeHistoryID)
                        .DefaultIfEmpty()
                        .Max() + 1;

                    ch.Action = action;
                    ch.ArticleID = this.ArticleID;
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
        protected override void WriteRemoteVersion(SyncEntityVersion<TEntity, ISharedChangeHistory> versionInfo)
        {
            this.ChangeHistory.Lock.EnterWriteLock();

            try
            {
                var ch = (ISharedChangeHistory)new SharedChangeHistory();

                // Resolve pk.
                ch.ChangeHistoryID = this.ChangeHistory
                    .Select(c => c.ChangeHistoryID)
                    .DefaultIfEmpty()
                    .Max() + 1;

                ch.Action = versionInfo.Version.Action;
                ch.ArticleID = this.ArticleID;
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
    }
}
