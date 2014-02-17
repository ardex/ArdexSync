using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Ardex.Collections.Generic;
using Ardex.Reflection;
using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.TestClient.Tests.Filtered.Entities;

namespace Ardex.TestClient.Tests.Filtered
{
    public class FilteredTest : IDisposable
    {
        // Replicas.
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

        // Test.
        public async Task RunAsync()
        {
            this.Client1.InspectionCriteria.Insert(
                new InspectionCriteria {
                    CriteriaID = 1,
                    EntityGuid = this.Client1.SyncProviders.InspectionCriteria.NewSequentialID(),
                    Name = "Test",
                    OwnerReplicaID = this.Client1.ReplicaInfo.ReplicaID,
                    Sequence = 1
                }
            );

            await this.Client1Sync.SynchroniseDiffAsync();
            await this.Client2Sync.SynchroniseDiffAsync();

            Debug.Print(this.ToString(this.Server.InspectionCriteria));
            Debug.Print(this.ToString(this.Client1.InspectionCriteria));
            Debug.Print(this.ToString(this.Client2.InspectionCriteria));
        }

        private SyncOperation CreateSyncSession(Replica server, Replica client)
        {
            // 1. InspectionCriteria.
            var up1 = SyncOperation
                .Create(client.SyncProviders.InspectionCriteria, server.SyncProviders.InspectionCriteria)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionCriteria>());

            var dn1 = SyncOperation
                .Create(server.SyncProviders.InspectionCriteria, client.SyncProviders.InspectionCriteria)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionCriteria>());

            // 2. InspectionObservation.
            var up2 = SyncOperation
                .Create(client.SyncProviders.InspectionObservation, server.SyncProviders.InspectionObservation)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionObservation>());

            var dn2 = SyncOperation
                .Create(server.SyncProviders.InspectionObservation, client.SyncProviders.InspectionObservation)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionObservation>());

            // 3. InspectionValue.
            var up3 = SyncOperation
                .Create(client.SyncProviders.InspectionValue, server.SyncProviders.InspectionValue)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionValue>());

            var dn3 = SyncOperation
                .Create(server.SyncProviders.InspectionValue, client.SyncProviders.InspectionValue)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionValue>());

            // 4. ShortList.
            var up4 = SyncOperation
                .Create(client.SyncProviders.ShortList, server.SyncProviders.ShortList)
                .Filtered(ChangeHistoryFilters.Serialization<ShortList>());

            var dn4 = SyncOperation
                .Create(server.SyncProviders.ShortList, client.SyncProviders.ShortList)
                .Filtered(ChangeHistoryFilters.Serialization<ShortList>());

            // 5. ShortListItem.
            var up5 = SyncOperation
                .Create(client.SyncProviders.ShortListItem, server.SyncProviders.ShortListItem)
                .Filtered(ChangeHistoryFilters.Serialization<ShortListItem>());

            var dn5 = SyncOperation
                .Create(server.SyncProviders.ShortListItem, client.SyncProviders.ShortListItem)
                .Filtered(ChangeHistoryFilters.Serialization<ShortListItem>());

            // Chain operations to get two-way sync for each article.
            var inspectionCriteriaSync = SyncOperation.Chain(up1, dn1);
            var inspectionObservationSync = SyncOperation.Chain(up2, dn2);
            var inspectionValueSync = SyncOperation.Chain(up3, dn3);
            var shortListSync = SyncOperation.Chain(up4, dn4);
            var shortListItemSync = SyncOperation.Chain(up5, dn5);

            // Construct session.
            return SyncOperation.Chain(
                inspectionCriteriaSync,
                inspectionObservationSync,
                inspectionValueSync,
                shortListSync,
                shortListItemSync
            );
        }

        private string ToString<T>(IRepository<T> repository)
        {
            var mapping = new TypeMapping<T>();

            return string.Join(Environment.NewLine, repository.Select(e => mapping.ToString(e)));
        }

        public void Dispose()
        {
            this.Server.Dispose();
            this.Client1.Dispose();
            this.Client2.Dispose();
        }
    }
}
