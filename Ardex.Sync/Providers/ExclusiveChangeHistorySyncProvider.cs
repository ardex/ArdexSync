using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ardex.Sync.ChangeTracking;

namespace Ardex.Sync.Providers
{
    public class ExclusiveChangeHistorySyncProvider<TEntity> : ChangeHistorySyncProvider<TEntity, IChangeHistory>
    {
        protected override IEnumerable<IChangeHistory> FilteredChangeHistory
        {
            get { return this.ChangeHistory; }
        }

        public ExclusiveChangeHistorySyncProvider(
            SyncID replicaID,
            SyncRepository<TEntity> repository,
            SyncRepository<IChangeHistory> changeHistory,
            UniqueIdMapping<TEntity> entityIdMapping) : base(replicaID, repository, changeHistory, entityIdMapping)
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
            ch.ReplicaID = this.ReplicaID;
            ch.UniqueID = this.EntityIdMapping.Get(entity);

            // Resolve version.
            var timestamp = this.ChangeHistory
                .Where(c => c.ReplicaID == this.ReplicaID)
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
            ch.UniqueID = remoteVersionInfo.Version.UniqueID;
            ch.Timestamp = remoteVersionInfo.Version.Timestamp;

            return ch;
        }
    }
}
