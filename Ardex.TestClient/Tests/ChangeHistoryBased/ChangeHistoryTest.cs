using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Ardex.Reflection;
using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.Providers;
using Ardex.Sync.SyncLocks;

namespace Ardex.TestClient.Tests.ChangeHistoryBased
{
    public class ChangeHistoryTest : IDisposable
    {
        // Sync providers.
        public ChangeHistorySyncProvider<Dummy, ChangeHistory> Server { get; private set; }
        public ChangeHistorySyncProvider<Dummy, ChangeHistory> Client1 { get; private set; }
        public ChangeHistorySyncProvider<Dummy, ChangeHistory> Client2 { get; private set; }

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
            //var changeHistory = new SyncRepository<ChangeHistory>(new ReaderWriterSyncLock());

            this.Server = new CachingChangeHistorySyncProvider<Dummy, ChangeHistory>(
                serverInfo,
                new SyncRepository<Guid, Dummy>(d => d.EntityGuid),
                new SyncRepository<int, ChangeHistory>(ch => ch.ChangeHistoryID)
            );

            this.Client1 = new CachingChangeHistorySyncProvider<Dummy, ChangeHistory>(
                client1Info,
                new SyncRepository<Guid, Dummy>(d => d.EntityGuid),
                new SyncRepository<int, ChangeHistory>(ch => ch.ChangeHistoryID)
            );

            this.Client2 = new CachingChangeHistorySyncProvider<Dummy, ChangeHistory>(
                client2Info,
                new SyncRepository<Guid, Dummy>(d => d.EntityGuid),
                new SyncRepository<int, ChangeHistory>(ch => ch.ChangeHistoryID)
            );

            // Change tracking and conflict config.
            this.Server.CleanUpMetadata = false;
            this.Server.ConflictStrategy = SyncConflictStrategy.Winner;

            this.Client1.CleanUpMetadata = true;
            this.Client1.ConflictStrategy = SyncConflictStrategy.Loser;

            this.Client2.CleanUpMetadata = true;
            this.Client2.ConflictStrategy = SyncConflictStrategy.Loser;

            // Tell the sync to ignore the local PK 
            // and teach it how to generate them.
            this.EntityMapping = new TypeMapping<Dummy>().Without(d => d.DummyID);
            
            this.Server.EntityTypeMapping = this.EntityMapping;
            this.Server.PreInsertProcessing = dummy => dummy.DummyID = this.Server.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

            this.Client1.EntityTypeMapping = this.EntityMapping;
            this.Client1.PreInsertProcessing = dummy => dummy.DummyID = this.Client1.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;

            this.Client2.EntityTypeMapping = this.EntityMapping;

            this.Client2.PreInsertProcessing = dummy =>
            {
                //if (new Random().Next(1, 10) == 5)
                //{
                //    throw new Exception("Test");
                //}

                dummy.DummyID = this.Client2.Repository.Select(d => d.DummyID).DefaultIfEmpty().Max() + 1;
            };

            // Chain sync operations to produce an upload/download chain.
            var client1Upload   = SyncOperation.Create(this.Client1, this.Server).Filtered(ChangeHistoryFilters.Serialization<Dummy>());
            var client1Download = SyncOperation.Create(this.Server, this.Client1).Filtered(ChangeHistoryFilters.Serialization<Dummy>());
            var client2Upload   = SyncOperation.Create(this.Client2, this.Server).Filtered(ChangeHistoryFilters.Serialization<Dummy>());
            var client2Download = SyncOperation.Create(this.Server, this.Client2).Filtered(ChangeHistoryFilters.Serialization<Dummy>());

            // Chain uploads and downloads to produce complete sync sessions.
            this.Client1Sync = SyncOperation.Chain(client1Upload, client1Download);
            this.Client2Sync = SyncOperation.Chain(client2Upload, client2Download);
        }

        public async Task RunAsync()
        {
            const int NUM_ITERATIONS = 500;

            for (var iterations = 0; iterations < NUM_ITERATIONS; iterations++)
            {
                // Sync 1.
                var dummy1 = new Dummy {
                    EntityGuid = this.Server.NewSequentialID(),
                    Text = "First dummy"
                };

                var dummy2 = new Dummy {
                    EntityGuid = this.Server.NewSequentialID(),
                    Text = "Second dummy"
                };

                {
                    this.Server.Repository.Insert(dummy1);
                    this.Server.Repository.Insert(dummy2);

                    await this.ParallelSyncAsync();

                    this.DumpEqual();
                }

                // Let's create an update conflict.
                {
                    var d1 = this.Server.Repository.Single(d => d.EntityGuid == dummy1.EntityGuid);
                    var d2 = this.Client1.Repository.Single(d => d.EntityGuid == dummy1.EntityGuid);

                    d1.Text = "Server conflict";
                    d2.Text = "Client conflict";

                    this.Server.Repository.Update(d1);
                    this.Client1.Repository.Update(d2);

                    await this.ParallelSyncAsync();

                    this.DumpEqual();
                }

                // Sync 2.
                var dummy3 = new Dummy {
                    EntityGuid = this.Client1.NewSequentialID(),
                    Text = "Third dummy"
                };

                {
                    this.Client1.Repository.Insert(dummy3);

                    await this.ParallelSyncAsync();

                    this.DumpEqual();
                }

                // Sync 3.
                var dummy4 = new Dummy {
                    EntityGuid = this.Client1.NewSequentialID(),
                    Text = "Dummy 4"
                };

                {
                    var repo2Dummy2 = this.Client1.Repository.Single(d => d.EntityGuid == dummy2.EntityGuid);

                    repo2Dummy2.Text = "Second dummy upd repo 2";

                    this.Client1.Repository.Update(repo2Dummy2);
                    this.Client1.Repository.Insert(dummy4);

                    // Let's spice things up a bit by pushing things out furhter to the thread pool.
                    var t1 = Task.Run(() => this.ParallelSyncAsync());

                    var t2 = Task.Run(() => this.Server.Repository.Insert(new Dummy {
                        EntityGuid = this.Server.NewSequentialID(),
                        Text = "Dodgy concurrent insert"
                    }));

                    var t3 = Task.Run(() =>
                    {
                        using (this.Client2.Repository.SyncLock.WriteLock())
                        {
                            var repo3Dummy3 = this.Client2.Repository.Single(d => d.EntityGuid == dummy2.EntityGuid);

                            repo3Dummy3.Text = "Dodgy concurrent update";

                            this.Client2.Repository.Update(repo3Dummy3);
                        }
                    });

                    await Task.WhenAll(t1, t2, t3);

                    this.DumpEqual();
                }

                // Sync 4, 5.
                var dummy5 = new Dummy {
                    EntityGuid = this.Client2.NewSequentialID(),
                    Text = "Client 2 dummy"
                };

                {
                    this.Client2.Repository.Insert(dummy5);

                    await this.ParallelSyncAsync();

                    this.DumpEqual();
                }

                // Sync 6, 7.
                var dummy6 = new Dummy {
                    EntityGuid = this.Client2.NewSequentialID(),
                    Text = "Dummy 6"
                };

                {
                    this.Client2.Repository.Insert(dummy6);

                    await this.SequentialSyncAsync();

                    var serverDummy = this.Server.Repository.Single(d => d.EntityGuid == dummy6.EntityGuid);

                    serverDummy.Text = "Dummy 6, server modified";

                    this.Server.Repository.Update(serverDummy);

                    await this.ParallelSyncAsync();

                    this.DumpEqual();
                }
            }

            // Done.
            this.DumpContents();
        }

        private async Task ParallelSyncAsync()
        {
            try
            {
                await Task.WhenAll(this.Client1Sync.SynchroniseDiffAsync(), this.Client2Sync.SynchroniseDiffAsync());
            }
            catch (Exception ex)
            {
                if (string.Equals(ex.Message, "Test"))
                {
                    Debug.Print("Test exception caught.");
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task SequentialSyncAsync()
        {
            await this.Client1Sync.SynchroniseDiffAsync();
            await this.Client2Sync.SynchroniseDiffAsync();
        }

        private void DumpContents()
        {
            Debug.Print("SERVER");
            Debug.Print(this.Server.Repository.ContentsDescription());
            Debug.Print("CLIENT 1");
            Debug.Print(this.Client1.Repository.ContentsDescription());
            Debug.Print("CLIENT 2");
            Debug.Print(this.Client2.Repository.ContentsDescription());
        }

        private void DumpEqual()
        {
            this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client1.Repository.OrderBy(d => d.EntityGuid), this.EntityMapping.EqualityComparer);
            this.DumpEqual(this.Server.Repository.OrderBy(d => d.EntityGuid), this.Client2.Repository.OrderBy(d => d.EntityGuid), this.EntityMapping.EqualityComparer);
        }

        private void DumpEqual<T>(IEnumerable<T> repo1, IEnumerable<T> repo2, IEqualityComparer<T> comparer)
        {
            Debug.Print("Equal: {0}.", repo1.SequenceEqual(repo2), comparer);
            Debug.Print("---");
        }

        public void Dispose()
        {
            this.Client1Sync.Dispose();
            this.Client2Sync.Dispose();

            this.Server.Dispose();
            this.Client1.Dispose();
            this.Client2.Dispose();
        }
    }
}
