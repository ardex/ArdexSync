using System;
using System.Collections.Generic;
using System.Linq;

using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;

namespace Ardex.Sync.Providers
{
    public class ExclusiveChangeHistorySyncProvider<TEntity> : ChangeHistorySyncProvider<TEntity, IChangeHistory>
    {
        protected override IEnumerable<IChangeHistory> FilteredChangeHistory
        {
            get { return this.ChangeHistory; }
        }

        public ExclusiveChangeHistorySyncProvider(
            SyncReplicaInfo replicaInfo,
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistory,
            SyncGuidMapping<TEntity> entityGuidMapping) : base(replicaInfo, repository, changeHistory, entityGuidMapping)
        {
            
        }

        protected override IChangeHistory CreateChangeHistoryForLocalChange(TEntity entity, ChangeHistoryAction action)
        {
            var ch = (IChangeHistory)new ChangeHistory();

            // Resolve pk.
            ch.ChangeHistoryID = this.ChangeHistory
                .Select(c => c.ChangeHistoryID)
                .DefaultIfEmpty()
                .Max() + 1;

            ch.Action = action;
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

        protected override IChangeHistory CreateChangeHistoryForRemoteChange(SyncEntityVersion<TEntity, IChangeHistory> remoteVersionInfo)
        {
            var ch = (IChangeHistory)new ChangeHistory();

            // Resolve pk.
            ch.ChangeHistoryID = this.ChangeHistory
                .Select(c => c.ChangeHistoryID)
                .DefaultIfEmpty()
                .Max() + 1;

            ch.Action = remoteVersionInfo.Version.Action;
            ch.ReplicaID = remoteVersionInfo.Version.ReplicaID;
            ch.EntityGuid = remoteVersionInfo.Version.EntityGuid;
            ch.Timestamp = remoteVersionInfo.Version.Timestamp;

            return ch;
        }
    }
}
