using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Ardex.Linq.Expressions;
using Ardex.Sync;
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
            await this.Test1Async();
            Debug.Print("After test 1:");
            this.Dump();

            await this.Test2Async();
            Debug.Print("After test 2:");
            this.Dump();

            await this.Test3Async();
            Debug.Print("After test 3:");
            this.Dump();

            Debug.Print("Client 2 change history:");
            Debug.Print(this.Client2.ChangeHistory.ContentsDescription());
        }

        private async Task Test1Async()
        {
            this.Client1.InspectionCriteria.Insert(
                new InspectionCriteria {
                    CriteriaID = 1,
                    EntityGuid = this.Client1.SyncProviders.InspectionCriteria.NewSequentialID(),
                    Name = "Score",
                    OwnerReplicaID = this.Client1.ReplicaInfo.ReplicaID,
                    Sequence = 1
                }
            );

            var sequence = 1;
            var valueID = 1;

            for (var i = 10; i > 0; i--)
            {
                this.Client1.InspectionValues.Insert(
                    new InspectionValue {
                        ValueID = valueID++,
                        EntityGuid = this.Client1.SyncProviders.InspectionValue.NewSequentialID(),
                        Name = "Score",
                        CriteriaID = 1,
                        OwnerReplicaID = this.Client1.ReplicaInfo.ReplicaID,
                        Sequence = sequence++
                    }
                );
            }

            this.Client1.ShortLists.Insert(new ShortList {
                ShortListID = 1,
                Name = "Test",
                Sequence = 1,
                OwnerReplicaID = this.Client1.ReplicaInfo.ReplicaID,
                EntityGuid = this.Client1.SyncProviders.ShortList.NewSequentialID()
            });

            this.Client1.ShortListItems.Insert(new ShortListItem {
                ShortListItemID = 1,
                ShortListID = 1,
                EntityGuid = this.Client1.SyncProviders.ShortList.NewSequentialID(),
                HorseID = 1,
                OwnerReplicaID = this.Client1.ReplicaInfo.ReplicaID
            });

            await this.ParallelSyncAsync();
            await this.ParallelSyncAsync();
        }

        private async Task Test2Async()
        {
            this.Client1.ShortListPermissions.Insert(
                new ShortListPermission {
                    PermissionID = 1,
                    GrantorReplicaID = this.Client1.ReplicaInfo.ReplicaID,
                    GranteeReplicaID = this.Client2.ReplicaInfo.ReplicaID,
                    ShortListID = 1,
                    EntityGuid = this.Client1.SyncProviders.ShortListPermission.NewSequentialID()
                }
            );

            await this.ParallelSyncAsync();
            await this.ParallelSyncAsync();
        }

        private async Task Test3Async()
        {
            this.Client1.InspectionObservations.Insert(new InspectionObservation {
                Created = DateTime.Now,
                EntityGuid = this.Client1.SyncProviders.InspectionObservation.NewSequentialID(),
                HorseID = 1,
                Flag = true,
                ObservationID = 1,
                OwnerReplicaID = this.Client1.ReplicaInfo.ReplicaID,
                SubcategoryID = 1,
                Text = "Zzz"
            });

            await this.ParallelSyncAsync();
            await this.ParallelSyncAsync();
        }

        private Task ParallelSyncAsync()
        {
            return Task.WhenAll(
                this.Client1Sync.SynchroniseDiffAsync(),
                this.Client2Sync.SynchroniseDiffAsync()
            );
        }

        private void Dump()
        {
            foreach (var replica in new[] { this.Server, this.Client1, this.Client2 })
            {
                Debug.Print(replica.ReplicaInfo.Name);

                var print = new Action<string>(s => { if (!string.IsNullOrEmpty(s)) Debug.Print(s); });

                print(replica.InspectionCriteria.ContentsDescription());
                print(replica.InspectionValues.ContentsDescription());
                print(replica.InspectionObservations.ContentsDescription());
                print(replica.ShortLists.ContentsDescription());
                print(replica.ShortListItems.ContentsDescription());
                print(replica.ShortListPermissions.ContentsDescription());
            }
        }

        private SyncOperation CreateSyncSession(Replica server, Replica client)
        {
            // 0. ShortListPermission.
            var shortListPermissionUpload = SyncOperation
                .Create(client.SyncProviders.ShortListPermission, server.SyncProviders.ShortListPermission)
                .Filtered(ChangeHistoryFilters.Serialization<ShortListPermission>());

            var shortListPermissionDownload = SyncOperation
                .Create(server.SyncProviders.ShortListPermission, client.SyncProviders.ShortListPermission)
                .Filtered(changes => changes.Where(p => p.Entity.GranteeReplicaID == client.ReplicaInfo.ReplicaID)) // Only download own permissions.
                .Filtered(ChangeHistoryFilters.Serialization<ShortListPermission>());

            // 1. InspectionCriteria.
            var inspectionCriteriaUpload = SyncOperation
                .Create(client.SyncProviders.InspectionCriteria, server.SyncProviders.InspectionCriteria)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionCriteria>());

            var inspectionCriteriaDownload = SyncOperation
                .Create(server.SyncProviders.InspectionCriteria, client.SyncProviders.InspectionCriteria)
                .Filtered(changes =>
                    (from p in server.ShortListPermissions where p.GranteeReplicaID == client.ReplicaInfo.ReplicaID
                     from sl in server.ShortLists where sl.OwnerReplicaID == p.GrantorReplicaID
                     from sli in server.ShortListItems where sli.ShortListID == sl.ShortListID && sli.OwnerReplicaID == p.GrantorReplicaID
                     from io in server.InspectionObservations where io.HorseID == sli.HorseID && io.OwnerReplicaID == p.GrantorReplicaID
                     from iv in server.InspectionValues where iv.ValueID == io.SubcategoryID && iv.OwnerReplicaID == p.GrantorReplicaID
                     from ic in server.InspectionCriteria where ic.CriteriaID == iv.CriteriaID && ic.OwnerReplicaID == p.GrantorReplicaID
                     from change in changes where change.Entity.CriteriaID == ic.CriteriaID && change.Entity.OwnerReplicaID == p.GrantorReplicaID
                     select change).Distinct())
                .Filtered(ChangeHistoryFilters.Serialization<InspectionCriteria>());

            // 2. InspectionObservation.
            var inspectionObservationUpload = SyncOperation
                .Create(client.SyncProviders.InspectionObservation, server.SyncProviders.InspectionObservation)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionObservation>());

            var inspectionObservationDownload = SyncOperation
                .Create(server.SyncProviders.InspectionObservation, client.SyncProviders.InspectionObservation)
                .Filtered(changes =>
                    (from p in server.ShortListPermissions where p.GranteeReplicaID == client.ReplicaInfo.ReplicaID
                     from sl in server.ShortLists where sl.OwnerReplicaID == p.GrantorReplicaID
                     from sli in server.ShortListItems where sli.ShortListID == sl.ShortListID && sli.OwnerReplicaID == p.GrantorReplicaID
                     from io in server.InspectionObservations where io.HorseID == sli.HorseID && io.OwnerReplicaID == p.GrantorReplicaID
                     from iv in server.InspectionValues where iv.ValueID == io.SubcategoryID && iv.OwnerReplicaID == p.GrantorReplicaID
                     from ic in server.InspectionCriteria where ic.CriteriaID == iv.CriteriaID && ic.OwnerReplicaID == p.GrantorReplicaID
                     from change in changes where change.Entity.ObservationID == io.ObservationID && change.Entity.OwnerReplicaID == p.GrantorReplicaID
                     select change).Distinct())
                .Filtered(ChangeHistoryFilters.Serialization<InspectionObservation>());

            // 3. InspectionValue.
            var inspectionValueUpload = SyncOperation
                .Create(client.SyncProviders.InspectionValue, server.SyncProviders.InspectionValue)
                .Filtered(ChangeHistoryFilters.Serialization<InspectionValue>());

            var inspectionValueDownload = SyncOperation
                .Create(server.SyncProviders.InspectionValue, client.SyncProviders.InspectionValue)
                .Filtered(changes =>
                    (from p in server.ShortListPermissions where p.GranteeReplicaID == client.ReplicaInfo.ReplicaID
                     from sl in server.ShortLists where sl.OwnerReplicaID == p.GrantorReplicaID
                     from sli in server.ShortListItems where sli.ShortListID == sl.ShortListID && sli.OwnerReplicaID == p.GrantorReplicaID
                     from io in server.InspectionObservations where io.HorseID == sli.HorseID && io.OwnerReplicaID == p.GrantorReplicaID
                     from iv in server.InspectionValues where iv.ValueID == io.SubcategoryID && iv.OwnerReplicaID == p.GrantorReplicaID
                     from ic in server.InspectionCriteria where ic.CriteriaID == iv.CriteriaID && ic.OwnerReplicaID == p.GrantorReplicaID
                     from change in changes where change.Entity.ValueID == iv.ValueID && change.Entity.OwnerReplicaID == p.GrantorReplicaID
                     select change).Distinct())
                .Filtered(ChangeHistoryFilters.Serialization<InspectionValue>());

            // 5. ShortList.
            var shortListUpload = SyncOperation
                .Create(client.SyncProviders.ShortList, server.SyncProviders.ShortList)
                .Filtered(ChangeHistoryFilters.Serialization<ShortList>());

            var shortListDownload = SyncOperation
                .Create(server.SyncProviders.ShortList, client.SyncProviders.ShortList)
                .Filtered(changes =>
                    (from p in server.ShortListPermissions where p.GranteeReplicaID == client.ReplicaInfo.ReplicaID
                     from sl in server.ShortLists where sl.OwnerReplicaID == p.GrantorReplicaID
                     from change in changes where change.Entity.ShortListID == sl.ShortListID && change.Entity.OwnerReplicaID == p.GrantorReplicaID
                     select change).Distinct())
                .Filtered(ChangeHistoryFilters.Serialization<ShortList>());

            // 6. ShortListItem.
            var shortListItemUpload = SyncOperation
                .Create(client.SyncProviders.ShortListItem, server.SyncProviders.ShortListItem)
                .Filtered(ChangeHistoryFilters.Serialization<ShortListItem>());

            var shortListItemDownload = SyncOperation
                .Create(server.SyncProviders.ShortListItem, client.SyncProviders.ShortListItem)
                .Filtered(changes =>
                    (from p in server.ShortListPermissions where p.GranteeReplicaID == client.ReplicaInfo.ReplicaID
                     from sl in server.ShortLists where sl.OwnerReplicaID == p.GrantorReplicaID
                     from sli in server.ShortListItems where sli.ShortListID == sl.ShortListID && sli.OwnerReplicaID == p.GrantorReplicaID
                     from change in changes where change.Entity.ShortListItemID == sli.ShortListItemID && change.Entity.OwnerReplicaID == p.GrantorReplicaID
                     select change).Distinct())
                .Filtered(ChangeHistoryFilters.Serialization<ShortListItem>());

            // Chain operations to get two-way sync for each article.
            var shortListPermissionSync = SyncOperation.Chain(shortListPermissionUpload, shortListPermissionDownload);
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
                shortListItemSync,
                shortListPermissionSync
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
