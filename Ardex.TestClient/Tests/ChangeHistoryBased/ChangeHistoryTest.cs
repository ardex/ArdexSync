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
                var serverInfo = new SyncReplicaInfo(-1, "Server");
                var client1Info = new SyncReplicaInfo(1, "Client 1");
                var client2Info = new SyncReplicaInfo(2, "Client 2");

                // In-memory storage.
                var repo1 = new SyncRepository<Dummy>();
                var repo2 = new SyncRepository<Dummy>();
                var repo3 = new SyncRepository<Dummy>();

                // Sync providers.
                var server = new ExclusiveChangeHistorySyncProvider<Dummy>(serverInfo, repo1, new SyncRepository<IChangeHistory>(), d => d.EntityGuid);
                var client1 = new ExclusiveChangeHistorySyncProvider<Dummy>(client1Info, repo2, new SyncRepository<IChangeHistory>(), d => d.EntityGuid);
                var client2 = new ExclusiveChangeHistorySyncProvider<Dummy>(client2Info, repo3, new SyncRepository<IChangeHistory>(), d => d.EntityGuid);

                server.CleanUpMetadata = false;
                server.ConflictStrategy = SyncConflictStrategy.Winner;

                client1.CleanUpMetadata = true;
                client1.ConflictStrategy = SyncConflictStrategy.Loser;

                client2.CleanUpMetadata = true;
                client2.ConflictStrategy = SyncConflictStrategy.Loser;

                // Tell the sync to ignore the local PK
                // and teach it how to generate them.
                server.EntityTypeMapping.Exclude(d => d.DummyID);
                server.EntityLocalKeyGenerator = dummy => dummy.DummyID = server.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

                client1.EntityTypeMapping.Exclude(d => d.DummyID);
                client1.EntityLocalKeyGenerator = dummy => dummy.DummyID = client1.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

                client2.EntityTypeMapping.Exclude(d => d.DummyID);
                client2.EntityLocalKeyGenerator = dummy => dummy.DummyID = client2.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

                // Prepare comparer which knows to ignore the DummyID column.
                var comparer = new TypeMapping<Dummy>().Exclude(d => d.DummyID).EqualityComparer;

                // Chain sync operations to produce an upload/download chain.
                var client1Upload = SyncOperation.Create(client1, server).Filtered(ChangeHistoryFilters.ExclusiveChangeHistoryFilter);
                var client1Download = SyncOperation.Create(server, client1).Filtered(ChangeHistoryFilters.ExclusiveChangeHistoryFilter);
                var client2Upload = SyncOperation.Create(client2, server).Filtered(ChangeHistoryFilters.ExclusiveChangeHistoryFilter);
                var client2Download = SyncOperation.Create(server, client2).Filtered(ChangeHistoryFilters.ExclusiveChangeHistoryFilter);
                var client1ToClient2 = SyncOperation.Create(client1, client2).Filtered(ChangeHistoryFilters.ExclusiveChangeHistoryFilter);
                var client2ToClient1 = SyncOperation.Create(client2, client1).Filtered(ChangeHistoryFilters.ExclusiveChangeHistoryFilter);
                //var client1Upload = SyncOperation.Create(client1, server).Filtered(this.SharedChangeHistoryFilter);
                //var client1Download = SyncOperation.Create(server, client1).Filtered(this.SharedChangeHistoryFilter);
                //var client2Upload = SyncOperation.Create(client2, server).Filtered(this.SharedChangeHistoryFilter);
                //var client2Download = SyncOperation.Create(server, client2).Filtered(this.SharedChangeHistoryFilter);
                //var client1ToClient2 = SyncOperation.Create(client1, client2).Filtered(this.SharedChangeHistoryFilter);
                //var client2ToClient1 = SyncOperation.Create(client2, client1).Filtered(this.SharedChangeHistoryFilter);

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
                        repo1.Insert(dummy1);
                        repo1.Insert(dummy2);

                        //await client1Sync.SynchroniseDiffAsync();
                        //await client2Sync.SynchroniseDiffAsync();
                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo2.OrderBy(d => d.EntityGuid), comparer);
                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo3.OrderBy(d => d.EntityGuid), comparer);
                    }

                    // Let's create an update conflict.
                    {
                        var d1 = repo1.Single(d => d.EntityGuid == dummy1.EntityGuid);
                        var d2 = repo2.Single(d => d.EntityGuid == dummy1.EntityGuid);

                        d1.Text = "Server conflict";
                        d2.Text = "Client conflict";

                        repo1.Update(d1);
                        repo2.Update(d2);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo2.OrderBy(d => d.EntityGuid), comparer);
                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo3.OrderBy(d => d.EntityGuid), comparer);
                    }

                    // Sync 2.
                    var dummy3 = new Dummy
                    {
                        EntityGuid = new SyncGuid(client1Info.ReplicaID, client1DummyID++),
                        Text = "Third dummy"
                    };

                    {
                        repo2.Insert(dummy3);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo2.OrderBy(d => d.EntityGuid), comparer);
                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo3.OrderBy(d => d.EntityGuid), comparer);
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

                        var repo2Dummy2 = repo2.Single(d => d.EntityGuid == dummy2.EntityGuid);

                        repo2Dummy2.Text = "Second dummy upd repo 2";

                        repo2.Update(repo2Dummy2);
                        repo2.Insert(dummy4);

                        // Let's spice things up a bit by pushing things out furhter to the thread pool.
                        var t1 = await Task.Run(async () => await client1Sync.SynchroniseDiffAsync());
                        var t2 = await Task.Run(async () => await client2Sync.SynchroniseDiffAsync());

                        var t3 = Task.Run(() => repo1.Insert(new Dummy
                        {
                            EntityGuid = new SyncGuid(serverInfo.ReplicaID, serverDummyID++),
                            Text = "Dodgy concurrent insert"
                        }));

                        var t4 = Task.Run(() =>
                        {
                            repo3.Lock.EnterWriteLock();

                            try
                            {
                                var repo3Dummy3 = repo3.Single(d => d.EntityGuid == dummy2.EntityGuid);

                                repo3Dummy3.Text = "Dodgy concurrent update";

                                repo3.Update(repo3Dummy3);
                            }
                            finally
                            {
                                repo3.Lock.ExitWriteLock();
                            }
                        });

                        //await Task.WhenAll(t1, t2, t3, t4);

                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo2.OrderBy(d => d.EntityGuid), comparer);
                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo3.OrderBy(d => d.EntityGuid), comparer);
                    }

                    // Sync 4, 5.
                    var dummy5 = new Dummy
                    {
                        EntityGuid = new SyncGuid(client2Info.ReplicaID, client2DummyID++),
                        Text = "Client 2 dummy"
                    };

                    {
                        repo3.Insert(dummy5);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo2.OrderBy(d => d.EntityGuid), comparer);
                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo3.OrderBy(d => d.EntityGuid), comparer);
                    }

                    // Sync 6, 7.
                    var dummy6 = new Dummy
                    {
                        EntityGuid = new SyncGuid(client2Info.ReplicaID, client2DummyID++),
                        Text = "Dummy 6"
                    };

                    {
                        repo3.Insert(dummy6);

                        //await clientClientSync.SynchroniseDiffAsync();
                        await client1Sync.SynchroniseDiffAsync();
                        await client2Sync.SynchroniseDiffAsync();

                        var serverDummy = repo1.Single(d => d.EntityGuid == dummy6.EntityGuid);

                        serverDummy.Text = "Dummy 6, server modified";

                        repo1.Update(serverDummy);

                        await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo2.OrderBy(d => d.EntityGuid), comparer);
                        this.DumpEqual(repo1.OrderBy(d => d.EntityGuid), repo3.OrderBy(d => d.EntityGuid), comparer);
                    }
                }

                // Done.
                Debug.Print("SERVER");
                Debug.Print(repo1.ContentsDescription());
                Debug.Print("CLIENT 1");
                Debug.Print(repo2.ContentsDescription());
                Debug.Print("CLIENT 2");
                Debug.Print(repo3.ContentsDescription());

                sw.Stop();

                MessageBox.Show(string.Format("Done. Seconds elapsed: {0:0.#}.", sw.Elapsed.TotalSeconds));

                MessageBox.Show(string.Format(
                    "Sync complete. Repo 1 and 2 equal = {0}, Repo 2 and 3 equal = {1}.",
                    repo1.OrderBy(p => p.EntityGuid).SequenceEqual(repo2.OrderBy(p => p.EntityGuid), comparer),
                    repo2.OrderBy(p => p.EntityGuid).SequenceEqual(repo3.OrderBy(p => p.EntityGuid), comparer)));
            }
        }

        private void DumpEqual<T>(IEnumerable<T> repo1, IEnumerable<T> repo2, IEqualityComparer<T> comparer)
        {
            Debug.Print("Equal: {0}.", repo1.SequenceEqual(repo2), comparer);
            Debug.Print("---");
        }
    }
}
