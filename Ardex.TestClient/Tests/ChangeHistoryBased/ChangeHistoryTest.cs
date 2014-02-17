using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Ardex.Reflection;
using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.Providers;

namespace Ardex.TestClient.Tests.ChangeHistoryBased
{
    public class ChangeHistoryTest : IDisposable
    {
        // Sync providers.
        public ChangeHistorySyncProvider<Dummy> Server { get; private set; }
        public ChangeHistorySyncProvider<Dummy> Client1 { get; private set; }
        public ChangeHistorySyncProvider<Dummy> Client2 { get; private set; }

        // Sync operations.
        public SyncOperation Client1Sync { get; private set; }
        public SyncOperation Client2Sync { get; private set; }

        // Custom type mapping (ignoring DummyID column).
        public TypeMapping<Dummy> EntityMapping { get; private set; }

        // Set up.
        public ChangeHistoryTest()
        {
            // Replica ID's.
            var serverInfo  = new SyncReplicaInfo(255, "Server");
            var client1Info = new SyncReplicaInfo(1, "Client 1");
            var client2Info = new SyncReplicaInfo(2, "Client 2");

            // Sync providers / in-memory storage.
            this.Server = new ChangeHistorySyncProvider<Dummy>(
                serverInfo, new SyncRepository<Dummy>(), new SyncRepository<IChangeHistory>(), d => d.EntityGuid);

            this.Client1 = new ChangeHistorySyncProvider<Dummy>(
                client1Info, new SyncRepository<Dummy>(), new SyncRepository<IChangeHistory>(), d => d.EntityGuid);

            this.Client2 = new ChangeHistorySyncProvider<Dummy>(
                client2Info, new SyncRepository<Dummy>(), new SyncRepository<IChangeHistory>(), d => d.EntityGuid);

            // Change tracking and conflict config.
            this.Server.CleanUpMetadata = false;
            this.Server.ConflictStrategy = SyncConflictStrategy.Winner;

            this.Client1.CleanUpMetadata = true;
            this.Client1.ConflictStrategy = SyncConflictStrategy.Loser;

            this.Client2.CleanUpMetadata = true;
            this.Client2.ConflictStrategy = SyncConflictStrategy.Loser;

            // Tell the sync to ignore the local PK
            // and teach it how to generate them.
            this.EntityMapping = new TypeMapping<Dummy>().Exclude(d => d.DummyID);

            this.Server.EntityTypeMapping = this.EntityMapping;
            this.Server.PreInsertProcessing = dummy => dummy.DummyID = this.Server.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

            this.Client1.EntityTypeMapping = this.EntityMapping;
            this.Client1.PreInsertProcessing = dummy => dummy.DummyID = this.Client1.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

            this.Client2.EntityTypeMapping = this.EntityMapping;
            this.Client2.PreInsertProcessing = dummy => dummy.DummyID = this.Client2.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

            // Chain sync operations to produce an upload/download chain.
            var client1Upload   = SyncOperation.Create(this.Client1, this.Server).Filtered(ChangeHistoryFilters.Serialization);
            var client1Download = SyncOperation.Create(this.Server, this.Client1).Filtered(ChangeHistoryFilters.Serialization);
            var client2Upload   = SyncOperation.Create(this.Client2, this.Server).Filtered(ChangeHistoryFilters.Serialization);
            var client2Download = SyncOperation.Create(this.Server, this.Client2).Filtered(ChangeHistoryFilters.Serialization);

            // Chain uploads and downloads to produce complete sync sessions.
            this.Client1Sync = SyncOperation.Chain(client1Upload, client1Download);
            this.Client2Sync = SyncOperation.Chain(client2Upload, client2Download);
        }

        public async Task RunAsync()
        {
            // Mapping for our equality comparisons.
            var entityMapping = this.EntityMapping;

            // Rolling primary key
            // (6-byte long entity ids).
            var serverDummyID  = 1L;
            var client1DummyID = 1L;
            var client2DummyID = 1L;

            const int NUM_ITERATIONS = 1;

            for (var iterations = 0; iterations < NUM_ITERATIONS; iterations++)
            {
                // Sync 1.
                var dummy1 = new Dummy
                {
                    EntityGuid = new SyncGuid(this.Server.ReplicaInfo.ReplicaID, serverDummyID++),
                    Text = "First dummy"
                };

                var dummy2 = new Dummy
                {
                    EntityGuid = new SyncGuid(this.Server.ReplicaInfo.ReplicaID, serverDummyID++),
                    Text = "Second dummy"
                };

                {
                    this.Server.Repository.Insert(dummy1);
                    this.Server.Repository.Insert(dummy2);

                    //await client1Sync.SynchroniseDiffAsync();
                    //await client2Sync.SynchroniseDiffAsync();
                    await Task.WhenAll(this.Client1Sync.SynchroniseDiffAsync(), this.Client2Sync.SynchroniseDiffAsync());

                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                }

                // Let's create an update conflict.
                {
                    var d1 = this.Server.Repository.Single(d => d.EntityGuid == dummy1.EntityGuid);
                    var d2 = this.Client1.Repository.Single(d => d.EntityGuid == dummy1.EntityGuid);

                    d1.Text = "Server conflict";
                    d2.Text = "Client conflict";

                    this.Server.Repository.Update(d1);
                    this.Client1.Repository.Update(d2);

                    await Task.WhenAll(this.Client1Sync.SynchroniseDiffAsync(), this.Client2Sync.SynchroniseDiffAsync());

                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                }

                // Sync 2.
                var dummy3 = new Dummy
                {
                    EntityGuid = new SyncGuid(this.Client1.ReplicaInfo.ReplicaID, client1DummyID++),
                    Text = "Third dummy"
                };

                {
                    this.Client1.Repository.Insert(dummy3);

                    await Task.WhenAll(this.Client1Sync.SynchroniseDiffAsync(), this.Client2Sync.SynchroniseDiffAsync());

                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                }

                // Sync 3.
                var dummy4 = new Dummy
                {
                    EntityGuid = new SyncGuid(this.Client1.ReplicaInfo.ReplicaID, client1DummyID++),
                    Text = "Dummy 4"
                };

                {
                    //var repo1Dummy1 = repo1.Single(d => d.DummyID == dummy1.DummyID);

                    //repo1Dummy1.Text = "First dummy upd repo 1";

                    //repo1.Update(repo1Dummy1);

                    var repo2Dummy2 = this.Client1.Repository.Single(d => d.EntityGuid == dummy2.EntityGuid);

                    repo2Dummy2.Text = "Second dummy upd repo 2";

                    this.Client1.Repository.Update(repo2Dummy2);
                    this.Client1.Repository.Insert(dummy4);

                    // Let's spice things up a bit by pushing things out furhter to the thread pool.
                    var t1 = await Task.Run(async () => await this.Client1Sync.SynchroniseDiffAsync());
                    var t2 = await Task.Run(async () => await this.Client2Sync.SynchroniseDiffAsync());

                    var t3 = Task.Run(() => this.Server.Repository.Insert(new Dummy
                    {
                        EntityGuid = new SyncGuid(this.Server.ReplicaInfo.ReplicaID, serverDummyID++),
                        Text = "Dodgy concurrent insert"
                    }));

                    var t4 = Task.Run(() =>
                    {
                        this.Client2.Repository.Lock.EnterWriteLock();

                        try
                        {
                            var repo3Dummy3 = this.Client2.Repository.Single(d => d.EntityGuid == dummy2.EntityGuid);

                            repo3Dummy3.Text = "Dodgy concurrent update";

                            this.Client2.Repository.Update(repo3Dummy3);
                        }
                        finally
                        {
                            this.Client2.Repository.Lock.ExitWriteLock();
                        }
                    });

                    //await Task.WhenAll(t1, t2, t3, t4);

                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                }

                // Sync 4, 5.
                var dummy5 = new Dummy
                {
                    EntityGuid = new SyncGuid(this.Client2.ReplicaInfo.ReplicaID, client2DummyID++),
                    Text = "Client 2 dummy"
                };

                {
                    this.Client2.Repository.Insert(dummy5);

                    await Task.WhenAll(this.Client1Sync.SynchroniseDiffAsync(), this.Client2Sync.SynchroniseDiffAsync());

                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                }

                // Sync 6, 7.
                var dummy6 = new Dummy
                {
                    EntityGuid = new SyncGuid(this.Client2.ReplicaInfo.ReplicaID, client2DummyID++),
                    Text = "Dummy 6"
                };

                {
                    this.Client2.Repository.Insert(dummy6);

                    //await clientClientSync.SynchroniseDiffAsync();
                    await this.Client1Sync.SynchroniseDiffAsync();
                    await this.Client2Sync.SynchroniseDiffAsync();

                    var serverDummy = this.Server.Repository.Single(d => d.EntityGuid == dummy6.EntityGuid);

                    serverDummy.Text = "Dummy 6, server modified";

                    this.Server.Repository.Update(serverDummy);

                    await Task.WhenAll(this.Client1Sync.SynchroniseDiffAsync(), this.Client2Sync.SynchroniseDiffAsync());

                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client1.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                    this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client2.Repository.OrderBy(d => d.EntityGuid), entityMapping.EqualityComparer);
                }
            }

            // Done.
            Debug.Print("SERVER");
            Debug.Print(this.Server.Repository.ContentsDescription());
            Debug.Print("CLIENT 1");
            Debug.Print(this.Client1.Repository.ContentsDescription());
            Debug.Print("CLIENT 2");
            Debug.Print(this.Client2.Repository.ContentsDescription());
        }

        private void DumpEqual<T>(IEnumerable<T> repo1, IEnumerable<T> repo2, IEqualityComparer<T> comparer)
        {
            Debug.Print("Equal: {0}.", repo1.SequenceEqual(repo2), comparer);
            Debug.Print("---");
        }

        public void Dispose()
        {
            this.Server.Dispose();
            this.Client1.Dispose();
            this.Client2.Dispose();
        }
    }
}
