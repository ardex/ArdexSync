using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ardex.Reflection;
using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.Providers;

namespace Ardex.TestClient.Tests.ChangeHistoryBased
{
    public class ChangeHistoryTest
    {
        public async Task RunAsync()
        {
            var sw = Stopwatch.StartNew();

            {
                // --- BEGIN SYNC SETUP --- //
                var serverInfo  = new SyncReplicaInfo(255, "Server");
                var client1Info = new SyncReplicaInfo(1, "Client 1");
                var client2Info = new SyncReplicaInfo(2, "Client 2");

                // Sync providers / in-memory storage.
                var server  = new ExclusiveChangeHistorySyncProvider<Dummy>(
                    serverInfo, new SyncRepository<Dummy>(), new SyncRepository<IChangeHistory>(), d => d.EntityGuid);

                var client1 = new ExclusiveChangeHistorySyncProvider<Dummy>(
                    client1Info, new SyncRepository<Dummy>(), new SyncRepository<IChangeHistory>(), d => d.EntityGuid);

                var client2 = new ExclusiveChangeHistorySyncProvider<Dummy>(
                    client2Info, new SyncRepository<Dummy>(), new SyncRepository<IChangeHistory>(), d => d.EntityGuid);

                // Change tracking and conflict config.
                server.CleanUpMetadata = false;
                server.ConflictStrategy = SyncConflictStrategy.Winner;

                client1.CleanUpMetadata = true;
                client1.ConflictStrategy = SyncConflictStrategy.Loser;

                client2.CleanUpMetadata = true;
                client2.ConflictStrategy = SyncConflictStrategy.Loser;

                // Tell the sync to ignore the local PK
                // and teach it how to generate them.
                var entityMapping = new TypeMapping<Dummy>().Exclude(d => d.DummyID);

                server.EntityTypeMapping = entityMapping;
                server.PreInsertProcessing = dummy => dummy.DummyID = server.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

                client1.EntityTypeMapping = entityMapping;
                client1.PreInsertProcessing = dummy => dummy.DummyID = client1.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

                client2.EntityTypeMapping = entityMapping;
                client2.PreInsertProcessing = dummy => dummy.DummyID = client2.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

                // Chain sync operations to produce an upload/download chain.
                var client1Upload = SyncOperation.Create(client1, server).Filtered(ChangeHistoryFilters.Exclusive);
                var client1Download = SyncOperation.Create(server, client1).Filtered(ChangeHistoryFilters.Exclusive);
                var client2Upload = SyncOperation.Create(client2, server).Filtered(ChangeHistoryFilters.Exclusive);
                var client2Download = SyncOperation.Create(server, client2).Filtered(ChangeHistoryFilters.Exclusive);
                var client1ToClient2 = SyncOperation.Create(client1, client2).Filtered(ChangeHistoryFilters.Exclusive);
                var client2ToClient1 = SyncOperation.Create(client2, client1).Filtered(ChangeHistoryFilters.Exclusive);

                // Chain uploads and downloads to produce complete sync sessions.
                var client1Sync = SyncOperation.Chain(client1Upload, client1Download);
                var client2Sync = SyncOperation.Chain(client2Upload, client2Download);

                // --- END SYNC SETUP --- //

                // Rolling primary key
                // (6-byte long entity ids).
                var serverDummyID = 1L;
                var client1DummyID = 1L;
                var client2DummyID = 1L;

                const int NUM_ITERATIONS = 1;

                for (var iterations = 0; iterations < NUM_ITERATIONS; iterations++)
                {
                    // Sync 1.
                    var dummy1 = new Dummy
                    {
                        EntityGuid = new SyncGuid(serverInfo.ReplicaID, serverDummyID++),
                        Text = "First dummy"
                    };

                    var dummy2 = new Dummy
                    {
                        EntityGuid = new SyncGuid(serverInfo.ReplicaID, serverDummyID++),
                        Text = "Second dummy"
                    };

                    {
                        server.Repository.Insert(dummy1);
                        server.Repository.Insert(dummy2);

                        //await client1Sync.SynchroniseDiffAsync();
                        //await client2Sync.SynchroniseDiffAsync();
                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    }

                    // Let's create an update conflict.
                    {
                        var d1 = server.Repository.Single(d => d.EntityGuid == dummy1.EntityGuid);
                        var d2 = client1.Repository.Single(d => d.EntityGuid == dummy1.EntityGuid);

                        d1.Text = "Server conflict";
                        d2.Text = "Client conflict";

                        server.Repository.Update(d1);
                        client1.Repository.Update(d2);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    }

                    // Sync 2.
                    var dummy3 = new Dummy
                    {
                        EntityGuid = new SyncGuid(client1Info.ReplicaID, client1DummyID++),
                        Text = "Third dummy"
                    };

                    {
                        client1.Repository.Insert(dummy3);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    }

                    // Sync 3.
                    var dummy4 = new Dummy
                    {
                        EntityGuid = new SyncGuid(client1Info.ReplicaID, client1DummyID++),
                        Text = "Dummy 4"
                    };

                    {
                        //var repo1Dummy1 = repo1.Single(d => d.DummyID == dummy1.DummyID);

                        //repo1Dummy1.Text = "First dummy upd repo 1";

                        //repo1.Update(repo1Dummy1);

                        var repo2Dummy2 = client1.Repository.Single(d => d.EntityGuid == dummy2.EntityGuid);

                        repo2Dummy2.Text = "Second dummy upd repo 2";

                        client1.Repository.Update(repo2Dummy2);
                        client1.Repository.Insert(dummy4);

                        // Let's spice things up a bit by pushing things out furhter to the thread pool.
                        var t1 = await Task.Run(async () => await client1Sync.SynchroniseDiffAsync());
                        var t2 = await Task.Run(async () => await client2Sync.SynchroniseDiffAsync());

                        var t3 = Task.Run(() => server.Repository.Insert(new Dummy
                        {
                            EntityGuid = new SyncGuid(serverInfo.ReplicaID, serverDummyID++),
                            Text = "Dodgy concurrent insert"
                        }));

                        var t4 = Task.Run(() =>
                        {
                            client2.Repository.Lock.EnterWriteLock();

                            try
                            {
                                var repo3Dummy3 = client2.Repository.Single(d => d.EntityGuid == dummy2.EntityGuid);

                                repo3Dummy3.Text = "Dodgy concurrent update";

                                client2.Repository.Update(repo3Dummy3);
                            }
                            finally
                            {
                                client2.Repository.Lock.ExitWriteLock();
                            }
                        });

                        //await Task.WhenAll(t1, t2, t3, t4);

                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    }

                    // Sync 4, 5.
                    var dummy5 = new Dummy
                    {
                        EntityGuid = new SyncGuid(client2Info.ReplicaID, client2DummyID++),
                        Text = "Client 2 dummy"
                    };

                    {
                        client2.Repository.Insert(dummy5);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    }

                    // Sync 6, 7.
                    var dummy6 = new Dummy
                    {
                        EntityGuid = new SyncGuid(client2Info.ReplicaID, client2DummyID++),
                        Text = "Dummy 6"
                    };

                    {
                        client2.Repository.Insert(dummy6);

                        //await clientClientSync.SynchroniseDiffAsync();
                        await client1Sync.SynchroniseDiffAsync();
                        await client2Sync.SynchroniseDiffAsync();

                        var serverDummy = server.Repository.Single(d => d.EntityGuid == dummy6.EntityGuid);

                        serverDummy.Text = "Dummy 6, server modified";

                        server.Repository.Update(serverDummy);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                        this.DumpEqual(server.Repository.OrderBy(d => d.EntityGuid), client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    }
                }

                // Done.
                Debug.Print("SERVER");
                Debug.Print(server.Repository.ContentsDescription());
                Debug.Print("CLIENT 1");
                Debug.Print(client1.Repository.ContentsDescription());
                Debug.Print("CLIENT 2");
                Debug.Print(client2.Repository.ContentsDescription());

                sw.Stop();

                MessageBox.Show(string.Format("Done. Seconds elapsed: {0:0.#}.", sw.Elapsed.TotalSeconds));

                MessageBox.Show(string.Format(
                    "Sync complete. Repo 1 and 2 equal = {0}, Repo 2 and 3 equal = {1}.",
                    server.Repository.OrderBy(p => p.EntityGuid).SequenceEqual(client1.Repository.OrderBy(p => p.EntityGuid), entityMapping.EqualityComparer),
                    server.Repository.OrderBy(p => p.EntityGuid).SequenceEqual(client2.Repository.OrderBy(p => p.EntityGuid), entityMapping.EqualityComparer)));
            }
        }

        private void DumpEqual<T>(IEnumerable<T> repo1, IEnumerable<T> repo2, IEqualityComparer<T> comparer)
        {
            Debug.Print("Equal: {0}.", repo1.SequenceEqual(repo2), comparer);
            Debug.Print("---");
        }
    }
}
