using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ardex.Collections;
using Ardex.Sync;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.PropertyMapping;
using Ardex.Sync.Providers.Merge;
using Ardex.Sync.Providers.Simple;

namespace Ardex.TestClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.TestAsync();
        }

        private async void TestAsync()
        {
            this.button1.Enabled = false;

            try
            {
                var sw = Stopwatch.StartNew();

                {
                    // In-memory storage.
                    var repo1 = new SyncRepository<Dummy>();
                    var repo2 = new SyncRepository<Dummy>();
                    var repo3 = new SyncRepository<Dummy>();

                    // Change history storage.
                    var changeHistory1 = new SyncRepository<IChangeHistory>();
                    var changeHistory2 = new SyncRepository<IChangeHistory>();
                    var changeHistory3 = new SyncRepository<IChangeHistory>();
                    var sharedChangeHistory = new SyncRepository<ISharedChangeHistory>();

                    // Essential member mapping.
                    var uniqueIdMapping = new UniqueIdMapping<Dummy>(d => d.DummyID);

                    //repo1.ChangeTracking.SetUp(changeHistory1, "Server", uniqueIdMapping);
                    //repo2.ChangeTracking.SetUp(changeHistory2, "Client 1", uniqueIdMapping);
                    //repo3.ChangeTracking.SetUp(changeHistory3, "Client 2", uniqueIdMapping);

                    // Link entity repos with their change history repos.
                    //var trackingRegistration1 = new ChangeTrackingFactory("Server").Exclusive(repo1, changeHistory1, uniqueIdMapping);
                    //var trackingRegistration2 = new ChangeTrackingFactory("Client 1").Exclusive(repo2, changeHistory2, uniqueIdMapping);
                    //var trackingRegistration3 = new ChangeTrackingFactory("Client 2").Exclusive(repo3, changeHistory3, uniqueIdMapping);
                    //var trackingRegistration1 = new ChangeTrackingFactory("Server").Shared("Dummy_Server", repo1, sharedChangeHistory, uniqueIdMapping);
                    //var trackingRegistration2 = new ChangeTrackingFactory("Client 1").Shared("Dummy_Client1", repo2, sharedChangeHistory, uniqueIdMapping);
                    //var trackingRegistration3 = new ChangeTrackingFactory("Client 2").Shared("Dummy_Client2", repo3, sharedChangeHistory, uniqueIdMapping);

                    // Sync providers.
                    //var server = MergeSyncProvider.Create(trackingRegistration1);
                    //var client1 = MergeSyncProvider.Create(trackingRegistration2);
                    //var client2 = MergeSyncProvider.Create(trackingRegistration3);
                    var server = SyncProvider.Create("Server", repo1, changeHistory1, new UniqueIdMapping<Dummy>(d => d.DummyID));
                    var client1 = SyncProvider.Create("Client 1", repo2, changeHistory2, new UniqueIdMapping<Dummy>(d => d.DummyID));
                    var client2 = SyncProvider.Create("Client 2", repo3, changeHistory3, new UniqueIdMapping<Dummy>(d => d.DummyID));

                    //server.CleanUpMetadataAfterSync = false;
                    //client1.CleanUpMetadataAfterSync = true;
                    //client2.CleanUpMetadataAfterSync = true;
                    server.ConflictResolutionStrategy = SyncConflictStrategy.Winner;
                    client1.ConflictResolutionStrategy = SyncConflictStrategy.Loser;
                    client2.ConflictResolutionStrategy = SyncConflictStrategy.Loser;

                    // Change filter: emulate serialization/deserialization.
                    // This is not necessary in real-world scenarios.
                    var filter = new SyncFilter<Dummy, IChangeHistory>(
                        changes => changes.Select(
                            c => new SyncEntityVersion<Dummy, IChangeHistory>(
                                c.Entity.Clone(),
                                new ChangeHistory(c.Version)
                            )
                        )
                    );

                    //var filter = new SyncFilter<Dummy, ISharedChangeHistory>(
                    //    changes => changes.Select(
                    //        c => new SyncEntityVersion<Dummy, ISharedChangeHistory>(
                    //            c.Entity.Clone(),
                    //            new SharedChangeHistory(c.Version)
                    //        )
                    //    )
                    //);

                    // Chain sync operations to produce an upload/download chain.
                    var client1Upload = SyncOperation.Create(client1, server).Filtered(filter);
                    var client1Download = SyncOperation.Create(server, client1).Filtered(filter);
                    var client2Upload = SyncOperation.Create(client2, server).Filtered(filter);
                    var client2Download = SyncOperation.Create(server, client2).Filtered(filter);
                    var client1ToClient2 = SyncOperation.Create(client1, client2).Filtered(filter);
                    var client2ToClient1 = SyncOperation.Create(client2, client1).Filtered(filter);

                    // Chain uploads and downloads to produce complete sync sessions.
                    var client1Sync = SyncOperation.Chain(client1Upload, client1Download);
                    var client2Sync = SyncOperation.Chain(client2Upload, client2Download);

                    // Rolling primary key.
                    var dummyID = 1;

                    const int NUM_ITERATIONS = 1;

                    for (var iterations = 0; iterations < NUM_ITERATIONS; iterations++)
                    {
                        // Sync 1.
                        var dummy1 = new Dummy { DummyID = dummyID++, Text = "First dummy" };
                        var dummy2 = new Dummy { DummyID = dummyID++, Text = "Second dummy" };
                        {
                            repo1.Insert(dummy1);
                            repo1.Insert(dummy2);

                            //await client1Sync.SynchroniseDiffAsync();
                            //await client2Sync.SynchroniseDiffAsync();
                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo2.OrderBy(d => d.DummyID));
                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo3.OrderBy(d => d.DummyID));
                        }

                        // Let's create an update conflict.
                        {
                            var d1 = repo1.Single(d => d.DummyID == 1);
                            var d2 = repo2.Single(d => d.DummyID == 1);

                            d1.Text = "Server conflict";
                            d2.Text = "Client conflict";

                            repo1.Update(d1);
                            repo2.Update(d2);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo2.OrderBy(d => d.DummyID));
                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo3.OrderBy(d => d.DummyID));
                        }

                        // Sync 2.
                        var dummy3 = new Dummy { DummyID = dummyID++, Text = "Third dummy" };
                        {
                            repo2.Insert(dummy3);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo2.OrderBy(d => d.DummyID));
                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo3.OrderBy(d => d.DummyID));
                        }

                        // Sync 3.
                        var dummy4 = new Dummy { DummyID = dummyID++, Text = "Dummy 4" };
                        {
                            var repo1Dummy1 = repo1.Single(d => d.DummyID == dummy1.DummyID);

                            repo1Dummy1.Text = "First dummy upd repo 1";

                            repo1.Update(repo1Dummy1);

                            var repo2Dummy2 = repo2.Single(d => d.DummyID == dummy2.DummyID);

                            repo2Dummy2.Text = "Second dummy upd repo 2";

                            repo2.Update(repo2Dummy2);
                            repo2.Insert(dummy4);

                            // Let's spice things up a bit by pushing things out furhter to the thread pool.
                            var t1 = await Task.Run(async () => await client1Sync.SynchroniseDiffAsync());
                            var t2 = await Task.Run(async () => await client2Sync.SynchroniseDiffAsync());
                            var t3 = Task.Run(() => repo1.Insert(new Dummy { DummyID = dummyID++, Text = "Dodgy concurrent insert" }));

                            var t4 = Task.Run(() =>
                            {
                                //    repo3.Lock.EnterWriteLock();

                                //    try
                                //    {
                                //        var repo3Dummy3 = repo3.Single(d => d.DummyID == dummy2.DummyID);

                                //        repo3Dummy3.Text = "Dodgy concurrent update";

                                //        repo3.Update(repo3Dummy3);
                                //    }
                                //    finally
                                //    {
                                //        repo3.Lock.ExitWriteLock();
                                //    }
                            });

                            //await Task.WhenAll(t1, t2, t3, t4);

                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo2.OrderBy(d => d.DummyID));
                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo3.OrderBy(d => d.DummyID));
                        }

                        // Sync 4, 5.
                        var dummy5 = new Dummy { DummyID = dummyID++, Text = "Client 2 dummy" };
                        {
                            repo3.Insert(dummy5);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo2.OrderBy(d => d.DummyID));
                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo3.OrderBy(d => d.DummyID));
                        }

                        // Sync 6, 7.
                        var dummy6 = new Dummy { DummyID = dummyID++, Text = "Dummy 6" };
                        {
                            repo3.Insert(dummy6);

                            //await clientClientSync.SynchroniseDiffAsync();
                            await client1Sync.SynchroniseDiffAsync();
                            await client2Sync.SynchroniseDiffAsync();

                            var serverDummy = repo1.Single(d => d.DummyID == dummy6.DummyID);

                            serverDummy.Text = "Dummy 6, server modified";

                            repo1.Update(serverDummy);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());

                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo2.OrderBy(d => d.DummyID));
                            this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo3.OrderBy(d => d.DummyID));
                        }
                    }

                    // Done.
                    Debug.Print("SERVER");
                    Debug.Print(repo1.ToString());
                    Debug.Print("CLIENT 1");
                    Debug.Print(repo2.ToString());
                    Debug.Print("CLIENT 2");
                    Debug.Print(repo3.ToString());

                    sw.Stop();

                    MessageBox.Show(string.Format("Done. Seconds elapsed: {0:0.#}.", sw.Elapsed.TotalSeconds));
                    MessageBox.Show(string.Format(
                        "Sync complete. Repo 1 and 2 equal = {0}, Repo 2 and 3 equal = {1}.",

                    repo1.OrderBy(p => p.DummyID).SequenceEqual(repo2.OrderBy(p => p.DummyID)),
                    repo2.OrderBy(p => p.DummyID).SequenceEqual(repo3.OrderBy(p => p.DummyID))));
                }
            }
            finally
            {
                this.button1.Enabled = true;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;

            try
            {
                var repo1 = new SyncRepository<DummyPermission>();
                var repo2 = new SyncRepository<DummyPermission>();
                var repo3 = new SyncRepository<DummyPermission>();

                var uniqueIdMapping = new UniqueIdMapping<DummyPermission>(d => d.DummyPermissionID);
                var timestampMapping = new ComparableMapping<DummyPermission>(d => d.Timestamp);

                // Simulate network service.
                var server = new VersionDelegateSyncSource<DummyPermission>(new SyncID("Server"), (lastSeenTimestamp, ct) =>
                {
                    // Network delay.
                    Thread.Sleep(500);

                    return repo1
                        .Where(p => lastSeenTimestamp == null || p.Timestamp.CompareTo(lastSeenTimestamp) > 0)
                        .OrderBy(p => p.Timestamp)
                        .Select(p => SyncEntityVersion.Create(p, (IComparable)p.Timestamp));
                });

                var client1 = new VersionRepositorySyncProvider<DummyPermission>(new SyncID("Client 1"), repo2, uniqueIdMapping, timestampMapping);
                var client2 = new VersionRepositorySyncProvider<DummyPermission>(new SyncID("Client 2"), repo3, uniqueIdMapping, timestampMapping);
                var filter = new SyncFilter<DummyPermission, IComparable>(changes => changes.Select(c => SyncEntityVersion.Create(c.Entity, c.Version)));
                var client1Sync = SyncOperation.Create(server, client1).Filtered(filter);
                var client2Sync = SyncOperation.Create(server, client2).Filtered(filter);

                // Begin.
                var dummyPermissionID = 1;
                var t = new Timestamp(1);

                var permission1 = new DummyPermission { DummyPermissionID = dummyPermissionID++, Timestamp = t++ };
                {
                    repo1.Insert(permission1);

                    await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                }

                Debug.Print("Sync 1 finished.");

                var permission2 = new DummyPermission { DummyPermissionID = dummyPermissionID++, Timestamp = t++ };
                {
                    repo1.Insert(permission2);

                    permission1.SourceReplicaID = new SyncID("Zzz");
                    permission1.Expired = true;
                    permission1.Timestamp = t++;

                    repo1.Update(permission1);

                    await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                }

                // Done.
                Debug.Print("SERVER");
                Debug.Print(repo1.ToString());
                Debug.Print("CLIENT 1");
                Debug.Print(repo2.ToString());
                Debug.Print("CLIENT 2");
                Debug.Print(repo3.ToString());

                MessageBox.Show(string.Format(
                    "Sync complete.{0}Repo 1 and 2 equal = {1}{0}Repo 2 and 3 equal = {2}",
                    Environment.NewLine,
                    repo1.OrderBy(p => p.DummyPermissionID).SequenceEqual(repo2.OrderBy(p => p.DummyPermissionID)),
                    repo2.OrderBy(p => p.DummyPermissionID).SequenceEqual(repo3.OrderBy(p => p.DummyPermissionID))));
            }
            finally
            {
                this.button2.Enabled = true;
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            this.button3.Enabled = false;

            try
            {
                var dir1 = new FolderRepository(@"C:\dev\DirectorySyncTest\Dir 1");
                var dir2 = new FolderRepository(@"C:\dev\DirectorySyncTest\Dir 2");

                var changeHistory1 = new SyncRepository<IChangeHistory>();
                var changeHistory2 = new SyncRepository<IChangeHistory>();

                var tracking1 = new ChangeTrackingFactory("Dir 1").Exclusive(dir1, changeHistory1, new UniqueIdMapping<FileEntry>(f => f.FileName));
                var tracking2 = new ChangeTrackingFactory("Dir 2").Exclusive(dir2, changeHistory2, new UniqueIdMapping<FileEntry>(f => f.FileName));

                var provider1 = MergeSyncProvider.Create(tracking1);
                var provider2 = MergeSyncProvider.Create(tracking2);

                provider2.CleanUpMetadataAfterSync = true;

                var stage1 = SyncOperation.Create(provider1, provider2);
                var stage2 = SyncOperation.Create(provider2, provider1);
                var sync = SyncOperation.Chain(stage1, stage2);

                MessageBox.Show("Starting sync");

                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await sync.SynchroniseDiffAsync();
                }
            }
            finally
            {
                this.button3.Enabled = true;
            }
        }

        public void Dump<T>(IEnumerable<T> repository, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                Debug.Print(prefix);
            }

            Debug.Print("Count: {0}.", repository.Count());

            foreach (var entity in repository)
            {
                Debug.Print(entity.ToString());
            }

            Debug.Print("---");
        }

        public void DumpEqual<T>(IEnumerable<T> repo1, IEnumerable<T> repo2)
        {
            Debug.Print("Equal: {0}.", repo1.SequenceEqual(repo2));
            Debug.Print("---");
        }
    }

    public static class DbContextExtensions
    {
        public static void Insert<T>(this DbContext dx, T entity) where T : class
        {
            dx.Entry(entity).State = EntityState.Added;
            dx.SaveChanges();
        }

        public static void Update<T>(this DbContext dx, T entity) where T : class
        {
            dx.Entry(entity).State = EntityState.Modified;
            dx.SaveChanges();
        }

        public static void Delete<T>(this DbContext dx, T entity) where T : class
        {
            dx.Entry(entity).State = EntityState.Deleted;
            dx.SaveChanges();
        }

        public static void Clear<T>(this DbContext dx) where T : class
        {
            var set = dx.Set<T>();

            set.RemoveRange(set.AsEnumerable());
            dx.SaveChanges();
        }
    }

    public class Dummy : IEquatable<Dummy>
    {
        public int DummyID { get; set; }
        public string Text { get; set; }

        public override string ToString()
        {
            return string.Format("[ DummyID = {0}, Text = {1} ]", this.DummyID, this.Text);
        }

        public Dummy Clone()
        {
            return new Dummy
            {
                DummyID = this.DummyID,
                Text = this.Text
            };
        }

        public bool Equals(Dummy other)
        {
            if (other == null) return false;

            return
                this.DummyID == other.DummyID &&
                this.Text == other.Text;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as Dummy);
        }

        public override int GetHashCode()
        {
            return this.DummyID;
        }
    }

    public class DummyPermission : IEquatable<DummyPermission>
    {
        public int DummyPermissionID { get; set; }
        public SyncID SourceReplicaID { get; set; }
        public int SourceDummyID { get; set; }
        public SyncID DestinationReplicaID { get; set; }
        public bool Expired { get; set; }
        public Timestamp Timestamp { get; set; }

        public DummyPermission Clone()
        {
            return new DummyPermission
            {
                DummyPermissionID = this.DummyPermissionID,
                SourceReplicaID = this.SourceReplicaID,
                SourceDummyID = this.SourceDummyID,
                DestinationReplicaID = this.DestinationReplicaID,
                Expired = this.Expired,
                Timestamp = this.Timestamp
            };
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DummyPermission);
        }

        public bool Equals(DummyPermission other)
        {
            return
                this.DummyPermissionID == other.DummyPermissionID &&
                this.SourceReplicaID == other.SourceReplicaID &&
                this.SourceDummyID == other.SourceDummyID &&
                this.DestinationReplicaID == other.DestinationReplicaID &&
                this.Expired == other.Expired &&
                this.Timestamp == other.Timestamp;
        }

        public override int GetHashCode()
        {
            return this.DummyPermissionID;
        }
    }

    public abstract class DummyDbContext : DbContext
    {
        public DbSet<Dummy> Dummies { get; set; }
        public DbSet<ChangeHistory> ChangeHistory { get; set; }

        protected DummyDbContext(string connectionString) : base(new SqlConnection(connectionString), true) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dummy>()
                        .HasKey(d => d.DummyID);

            modelBuilder.Entity<Dummy>()
                        .Property(d => d.DummyID)
                        .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            modelBuilder.Entity<ChangeHistory>()
                        .HasKey(ch => ch.ChangeHistoryID);

            modelBuilder.Entity<ChangeHistory>()
                        .Property(ch => ch.ChangeHistoryID)
                        .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        }
    }

    public class ServerDbContext : DummyDbContext
    {
        static ServerDbContext()
        {
            Database.SetInitializer<ServerDbContext>(null);
        }

        public ServerDbContext(string connectionString) : base(connectionString) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Dummy>().ToTable("ServerDummy");
            modelBuilder.Entity<ChangeHistory>().ToTable("ServerDummy_ChangeHistory");
        }
    }

    public class Client1DbContext : DummyDbContext
    {
        static Client1DbContext()
        {
            Database.SetInitializer<Client1DbContext>(null);
        }

        public Client1DbContext(string connectionString) : base(connectionString) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Dummy>().ToTable("Client1Dummy");
            modelBuilder.Entity<ChangeHistory>().ToTable("Client1Dummy_ChangeHistory");
        }
    }

    public class Client2DbContext : DummyDbContext
    {
        static Client2DbContext()
        {
            Database.SetInitializer<Client2DbContext>(null);
        }

        public Client2DbContext(string connectionString) : base(connectionString) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Dummy>().ToTable("Client2Dummy");
            modelBuilder.Entity<ChangeHistory>().ToTable("Client2Dummy_ChangeHistory");
        }
    }
}
