using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ardex.Sync;
using Ardex.Sync.EntityMapping;
using Ardex.Sync.Providers;

namespace Ardex.TestClient.Tests.TimestampBased
{
    public class TimestampTest
    {
        public async Task RunAsync()
        {
            var serverInfo = new SyncReplicaInfo(0xFFFF, "Server");
            var client1Info = new SyncReplicaInfo(0x0001, "Client 1");
            var client2Info = new SyncReplicaInfo(0x0002, "Client 2");

            var repo1 = new SyncRepository<DummyPermission>();
            var repo2 = new SyncRepository<DummyPermission>();
            var repo3 = new SyncRepository<DummyPermission>();

            var timestampMapping = new Func<DummyPermission, Timestamp>(d => d.Timestamp);
            var comparer = new CustomComparer<Timestamp>((x, y) => x.CompareTo(y));

            //// Simulate network service.
            //var server = new SimpleCustomSyncProvider<DummyPermission, Timestamp>(
            //    new SyncID("Server"),
            //    () => SyncAnchor.Create(repo1, null, timestampMapping, comparer),
            //    (anchor, ct) =>
            //    {
            //        // Network delay.
            //        Thread.Sleep(500);

            //        // We know the other side won't have any
            //        // replica ID knowledge in a one-way sync.
            //        var lastSeenTimestamp = anchor[null];

            //        return repo1
            //            .Where(p =>
            //                lastSeenTimestamp == null ||
            //                p.Timestamp.CompareTo(lastSeenTimestamp) > 0)
            //            .OrderBy(p => p.Timestamp)
            //            .Select(p => SyncEntityVersion.Create(p, p.Timestamp));
            //    });

            var ownerIdMapping = new SyncEntityOwnerMapping<DummyPermission>(d => new SyncGuid(d.DummyPermissionID).ReplicaID);
            var server = new SimpleRepositorySyncProvider<DummyPermission, Guid, Timestamp>(serverInfo, repo1, d => d.DummyPermissionID, d => d.Timestamp, comparer, ownerIdMapping);
            var client1 = new SimpleRepositorySyncProvider<DummyPermission, Guid, Timestamp>(client1Info, repo2, d => d.DummyPermissionID, d => d.Timestamp, comparer, ownerIdMapping);
            var client2 = new SimpleRepositorySyncProvider<DummyPermission, Guid, Timestamp>(client2Info, repo3, d => d.DummyPermissionID, d => d.Timestamp, comparer, ownerIdMapping);
            var filter = new SyncFilter<DummyPermission, Timestamp>(changes => changes.Select(c => SyncEntityVersion.Create(c.Entity.Clone(), new Timestamp(c.Version))));

            // Sync ops.
            var client1Upload = SyncOperation.Create(client1, server).Filtered(filter);
            var client1Download = SyncOperation.Create(server, client1).Filtered(filter);
            var client2Upload = SyncOperation.Create(client2, server).Filtered(filter);
            var client2Download = SyncOperation.Create(server, client2).Filtered(filter);

            var client1Sync = SyncOperation.Chain(client1Upload, client1Download);
            var client2Sync = SyncOperation.Chain(client2Upload, client2Download);

            var nextTimestamp = new Func<SyncProvider<DummyPermission, Guid, Timestamp>, Timestamp>(provider =>
            {
                var maxTimestamp = provider.Repository
                    .Where(d => ownerIdMapping(d) == provider.ReplicaInfo.ReplicaID)
                    .Select(d => d.Timestamp)
                    .DefaultIfEmpty()
                    .Max();

                return maxTimestamp == null ? new Timestamp(1) : ++maxTimestamp;
            });

            // Begin.
            var permission1 = new DummyPermission
            {
                DummyPermissionID = Guid.Parse("00000001-0000-0000-0000-000000000001"),
                Timestamp = nextTimestamp(server),
                SourceReplicaID = server.ReplicaInfo.ReplicaID
            };
            {
                // Legal.
                repo1.Insert(permission1);

                await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
            }

            var permission2 = new DummyPermission
            {
                DummyPermissionID = Guid.Parse("00000002-0000-0000-0000-000000000001"),
                Timestamp = nextTimestamp(client1),
                SourceReplicaID = client1.ReplicaInfo.ReplicaID
            };
            {
                // Legal.
                repo2.Insert(permission2);

                await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
            }

            // Let's do a merge conflict.
            {
                // Illegal.
                var repo2Permission1 = server.Repository.Single(p => p.DummyPermissionID == permission1.DummyPermissionID);

                repo2Permission1.SourceReplicaID = server.ReplicaInfo.ReplicaID;
                repo2Permission1.Expired = true;
                repo2Permission1.Timestamp = nextTimestamp(server);

                repo2.Update(repo2Permission1);

                await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
            }

            // Done.
            Debug.Print("SERVER");
            Debug.Print(repo1.ContentsDescription());
            Debug.Print("CLIENT 1");
            Debug.Print(repo2.ContentsDescription());
            Debug.Print("CLIENT 2");
            Debug.Print(repo3.ContentsDescription());

            MessageBox.Show(string.Format(
                "Sync complete.{0}Repo 1 and 2 equal = {1}{0}Repo 2 and 3 equal = {2}",
                Environment.NewLine,
                repo1.OrderBy(p => p.DummyPermissionID).SequenceEqual(repo2.OrderBy(p => p.DummyPermissionID)),
                repo2.OrderBy(p => p.DummyPermissionID).SequenceEqual(repo3.OrderBy(p => p.DummyPermissionID))));
        }
    }
}
