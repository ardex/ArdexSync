using System;
using System.Collections.Generic;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public class SharedChangeHistorySyncProvider<TEntity> : ChangeHistorySyncProvider<TEntity, ISharedChangeHistory>
    {
        public short ArticleID { get; private set; }

        protected override IEnumerable<ISharedChangeHistory> FilteredChangeHistory
        {
            get
            {
                return this.ChangeHistory.Where(ch => ch.ArticleID == this.ArticleID);
            }
        }

        public SharedChangeHistorySyncProvider(
            SyncReplicaInfo replicaInfo,
            short articleID,
            SyncRepository<TEntity> repository,
            SyncRepository<ISharedChangeHistory> changeHistory,
            SyncGuidMapping<TEntity> entityGuidMapping) : base(replicaInfo, repository, changeHistory, entityGuidMapping)
        {
            this.ArticleID = articleID;
        }

        protected override ISharedChangeHistory CreateChangeHistoryForLocalChange(TEntity entity, ChangeHistoryAction action)
        {
            var ch = (ISharedChangeHistory)new SharedChangeHistory();

            // Resolve pk.
            ch.ChangeHistoryID = this.ChangeHistory
                .Select(c => c.ChangeHistoryID)
                .DefaultIfEmpty()
                .Max() + 1;

            ch.Action = action;
            ch.ArticleID = this.ArticleID;
            ch.ReplicaID = this.ReplicaInfo.ReplicaID;
            ch.EntityGuid = this.EntityGuidMapping.Get(entity);

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
        protected override ISharedChangeHistory CreateChangeHistoryForRemoteChange(SyncEntityVersion<TEntity, ISharedChangeHistory> versionInfo)
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
            ch.EntityGuid = versionInfo.Version.EntityGuid;
            ch.Timestamp = versionInfo.Version.Timestamp;

            return ch;
        }
    }
}
