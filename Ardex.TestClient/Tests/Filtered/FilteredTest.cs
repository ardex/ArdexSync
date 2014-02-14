using System;
using System.Linq;

using Ardex.Reflection;

using Ardex.Sync;
using Ardex.Sync.ChangeTracking;

using Ardex.TestClient.Tests.Filtered.Entities;

namespace Ardex.TestClient.Tests.Filtered
{
    public class FilteredTest : IDisposable
    {
        public Replica Server { get; private set; }
        public Replica Client1 { get; private set; }
        public Replica Client2 { get; private set; }

        // Sync operations.
        public SyncOperation Client1Sync { get; private set; }
        public SyncOperation Client2Sync { get; private set; }

        // Set up.
        public FilteredTest()
        {
            // Replica ID's.
            var serverInfo  = new SyncReplicaInfo(255, "Server");
            var client1Info = new SyncReplicaInfo(1, "Client 1");
            var client2Info = new SyncReplicaInfo(2, "Client 2");

            this.Server  = new Replica(serverInfo, false, SyncConflictStrategy.Winner);
            this.Client1 = new Replica(client1Info, true, SyncConflictStrategy.Loser);
            this.Client2 = new Replica(client2Info, true, SyncConflictStrategy.Loser);

            // Set up sync operations.
            this.Client1Sync = this.CreateSyncSession(this.Server, this.Client1);
            this.Client2Sync = this.CreateSyncSession(this.Server, this.Client2);
        }

        private SyncOperation CreateSyncSession(Replica server, Replica client)
        {
            // 1. InspectionCriteria.
            var inspectionCriteriaUpload = SyncOperation.Create(client.InspectionCriteria, server.InspectionCriteria).Filtered(this.Filter<InspectionCriteria>());
            var inspectionCriteriaDownload = SyncOperation.Create(server.InspectionCriteria, client.InspectionCriteria).Filtered(this.Filter<InspectionCriteria>());

            // 2. InspectionObservation.
            var inspectionObservationUpload = SyncOperation.Create(client.InspectionObservations, server.InspectionObservations).Filtered(this.Filter<InspectionObservation>());
            var inspectionObservationDownload = SyncOperation.Create(server.InspectionObservations, client.InspectionObservations).Filtered(this.Filter<InspectionObservation>());

            // 3. InspectionValue.
            var inspectionValueUpload = SyncOperation.Create(client.InspectionValues, server.InspectionValues).Filtered(this.Filter<InspectionValue>());
            var inspectionValueDownload = SyncOperation.Create(server.InspectionValues, client.InspectionValues).Filtered(this.Filter<InspectionValue>());

            // 4. ShortList.
            var shortListUpload = SyncOperation.Create(client.ShortLists, server.ShortLists).Filtered(this.Filter<ShortList>());
            var shortListDownload = SyncOperation.Create(server.ShortLists, client.ShortLists).Filtered(this.Filter<ShortList>());

            // 5. ShortListItem.
            var shortListItemUpload = SyncOperation.Create(client.ShortListItems, server.ShortListItems).Filtered(this.Filter<ShortListItem>());
            var shortListItemDownload = SyncOperation.Create(server.ShortListItems, client.ShortListItems).Filtered(this.Filter<ShortListItem>());

            // Chain operations to get two-way sync for each article.
            var inspectionCriteriaSync = SyncOperation.Chain(inspectionCriteriaUpload, inspectionCriteriaDownload);
            var inspectionObservationSync = SyncOperation.Chain(inspectionObservationUpload, inspectionObservationDownload);
            var inspectionValueSync = SyncOperation.Chain(inspectionValueUpload, inspectionValueDownload);
            var shortListSync = SyncOperation.Chain(shortListUpload, shortListDownload);
            var shortListItemSync = SyncOperation.Chain(shortListItemUpload, shortListItemDownload);

            // Construct session.
            return SyncOperation.Chain(
                inspectionCriteriaSync,
                inspectionObservationSync,
                inspectionValueSync,
                shortListSync,
                shortListItemSync
            );
        }

        private SyncFilter<TEntity, IChangeHistory> Filter<TEntity>() where TEntity : new()
        {
            var changeHistoryMapping = new TypeMapping<IChangeHistory>();
            var entityMapping = new TypeMapping<TEntity>();

            return new SyncFilter<TEntity, IChangeHistory>(
                changes => changes.Select(
                    version =>
                    {
                        var newEntity = new TEntity();
                        var newChangeHistory = (IChangeHistory)new ChangeHistory();

                        entityMapping.CopyValues(version.Entity, newEntity);
                        changeHistoryMapping.CopyValues(version.Version, newChangeHistory);

                        return SyncEntityVersion.Create(newEntity, newChangeHistory);
                    }
                )
            );
        }

        public void Dispose()
        {
            this.Server.Dispose();
            this.Client1.Dispose();
            this.Client2.Dispose();
        }
    }
}
