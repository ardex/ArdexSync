using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ardex.Collections;

using Ardex.Sync;
using Ardex.Sync.ChangeBased;
using Ardex.Sync.ChangeTracking;
using Ardex.Sync.EntityMapping;
using Ardex.Sync.TimestampBased;

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

                var cs = new SqlConnectionStringBuilder {
                    DataSource = @"BABUSHKA\SQL2008R2",
                    InitialCatalog = "ArdexSync",
                    IntegratedSecurity = true
                };

                using (var dx1 = new ServerDbContext(cs.ToString()))
                using (var dx2 = new Client1DbContext(cs.ToString()))
                using (var dx3 = new Client2DbContext(cs.ToString()))
                {
                    // Clear DB.
                    dx1.Clear<Dummy>();
                    dx1.Clear<ChangeHistory>();
                    dx2.Clear<Dummy>();
                    dx2.Clear<ChangeHistory>();
                    dx3.Clear<Dummy>();
                    dx3.Clear<ChangeHistory>();

                    // Node (replica) IDs.
                    var serverID = new SyncID("Server");
                    var client1ID = new SyncID("Client 1");
                    var client2ID = new SyncID("Client 2");

                    // In-memory storage.
                    var repo1 = new SyncRepositoryWithChangeTracking<Dummy>(dx1.Dummies);
                    var repo2 = new SyncRepositoryWithChangeTracking<Dummy>(dx2.Dummies);
                    var repo3 = new SyncRepositoryWithChangeTracking<Dummy>(dx3.Dummies);

                    // Change history storage.
                    var changeHistory1 = new SyncRepository<IChangeHistory>(dx1.ChangeHistory);
                    var changeHistory2 = new SyncRepository<IChangeHistory>(dx2.ChangeHistory);
                    var changeHistory3 = new SyncRepository<IChangeHistory>(dx3.ChangeHistory);

                    // Essential member mapping.
                    var uniqueIdMapping = new UniqueIdMapping<Dummy>(d => d.DummyID);

                    // Link entity repos with their change history repos.
                    new ChangeTrackingFactory(serverID).InstallChangeTracking(repo1, changeHistory1, uniqueIdMapping);
                    new ChangeTrackingFactory(client1ID).InstallChangeTracking(repo2, changeHistory2, uniqueIdMapping);
                    new ChangeTrackingFactory(client2ID).InstallChangeTracking(repo3, changeHistory3, uniqueIdMapping);

                    // Sync providers.
                    var server = new ChangeSyncRepositoryProvider<Dummy>(serverID, repo1, changeHistory1, uniqueIdMapping);
                    var client1 = new ChangeSyncRepositoryProvider<Dummy>(client1ID, repo2, changeHistory2, uniqueIdMapping) { CleanUpMetadataAfterSync = true };
                    var client2 = new ChangeSyncRepositoryProvider<Dummy>(client2ID, repo3, changeHistory3, uniqueIdMapping) { CleanUpMetadataAfterSync = true };

                    
                    // Chain sync operations to produce an upload/download chain.
                    var client1Upload = SyncOperation.Create(client1, server);
                    var client1Download = SyncOperation.Create(server, client1);
                    var client2Upload = SyncOperation.Create(client2, server);
                    var client2Download = SyncOperation.Create(server, client2);
                    var client1ToClient2 = SyncOperation.Create(client1, client2);
                    var client2ToClient1 = SyncOperation.Create(client2, client1);

                    foreach (var op in new[] {
                        client1Upload,
                        client1Download,
                        client2Upload,
                        client2Download,
                        client1ToClient2,
                        client2ToClient1 })
                    {
                        op.BatchSize = 100;

                        // Change filter: emulate serialization/deserialization.
                        // This is not necessary in real-world scenarios.
                        op.Filter = new SyncFilter<Change<Dummy>>(
                            changes => changes.Select(c => new Change<Dummy>(new ChangeHistory(c.ChangeHistory), c.Entity.Clone())));
                    }

                    // Chain uploads and downloads to produce complete sync sessions.
                    var client1Sync = SyncOperation.Chain(client1Upload, client1Download);
                    var client2Sync = SyncOperation.Chain(client2Upload, client2Download);
                    var clientClientSync = SyncOperation.Chain(client1ToClient2, client2ToClient1);

                    // Database backing for in-memory storage.
                    repo1.EntityInserted += dx1.Insert;
                    repo1.EntityUpdated += dx1.Update;
                    repo2.EntityInserted += dx2.Insert;
                    repo2.EntityUpdated += dx2.Update;
                    repo3.EntityInserted += dx3.Insert;
                    repo3.EntityUpdated += dx3.Update;
                    changeHistory1.EntityInserted += dx1.Insert;
                    changeHistory1.EntityUpdated += dx1.Update;
                    changeHistory1.EntityDeleted += dx1.Delete;
                    changeHistory2.EntityInserted += dx2.Insert;
                    changeHistory2.EntityUpdated += dx2.Update;
                    changeHistory2.EntityDeleted += dx2.Delete;
                    changeHistory3.EntityInserted += dx3.Insert;
                    changeHistory3.EntityUpdated += dx3.Update;
                    changeHistory3.EntityDeleted += dx3.Delete;

                    // Rolling primary key.
                    var dummyID = 1;

                    const int NUM_ITERATIONS = 100;

                    for (var iterations = 0; iterations < NUM_ITERATIONS; iterations++)
                    {
                        // Sync 1.
                        var dummy1 = new Dummy { DummyID = dummyID++, Text = "First dummy" };
                        var dummy2 = new Dummy { DummyID = dummyID++, Text = "Second dummy" };
                        {
                            repo1.Insert(dummy1);
                            repo1.Insert(dummy2);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                        }

                        // Sync 2.
                        var dummy3 = new Dummy { DummyID = dummyID++, Text = "Third dummy" };
                        {
                            repo2.Insert(dummy3);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
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

                            //client2.Repository.ObtainExclusiveLock();

                            var repo3Dummy3 = client2.Repository.Single(d => d.DummyID == dummy2.DummyID);

                            repo3Dummy3.Text = "Dodgy concurrent update";

                            // Let's spice things up a bit by pushing things out furhter to the thread pool.
                            await Task.WhenAll(
                                Task.Run(async () => await client1Sync.SynchroniseDiffAsync()),
                                Task.Run(async () => await client2Sync.SynchroniseDiffAsync()),
                                Task.Run(() => repo1.Insert(new Dummy { DummyID = dummyID++, Text = "Dodgy concurrent insert" })));
                                //Task.Run(() =>
                                //{
                                //    client2.Repository.Update(repo3Dummy3);
                                //    //client2.Repository.ReleaseExclusiveLock();
                                //}));
                        }

                        // Sync 4, 5.
                        var dummy5 = new Dummy { DummyID = dummyID++, Text = "Client 2 dummy" };
                        {
                            client2.Repository.Insert(dummy5);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                        }

                        // Sync 6, 7.
                        var dummy6 = new Dummy { DummyID = dummyID++, Text = "Dummy 6" };
                        {
                            client2.Repository.Insert(dummy6);

                            await clientClientSync.SynchroniseDiffAsync();
                            await client2Sync.SynchroniseDiffAsync();

                            var serverDummy = server.Repository.Single(d => d.DummyID == dummy6.DummyID);

                            serverDummy.Text = "Dummy 6, server modified";

                            server.Repository.Update(serverDummy);

                            await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                        }
                    }

                    // Final dump equal.
                    this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo2.OrderBy(d => d.DummyID));
                    this.DumpEqual(repo1.OrderBy(d => d.DummyID), repo3.OrderBy(d => d.DummyID));

                    // Done.
                    sw.Stop();

                    this.Text = "z";

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
                var timestampMapping = new TimestampMapping<DummyPermission>(d => d.Timestamp);

                // Simulate network service.
                var server = new TimestampSyncDelegateSource<DummyPermission>(new SyncID("Server"), (lastSeenTimestamp, ct) =>
                {
                    // Network delay.
                    Thread.Sleep(500);

                    return repo1
                        .Where(p => lastSeenTimestamp == null || p.Timestamp > lastSeenTimestamp)
                        .OrderBy(p => p.Timestamp)
                        .ToArray();
                });

                var client1 = new TimestampSyncRepositoryProvider<DummyPermission>(new SyncID("Client 1"), repo2, uniqueIdMapping, timestampMapping);
                var client2 = new TimestampSyncRepositoryProvider<DummyPermission>(new SyncID("Client 2"), repo3, uniqueIdMapping, timestampMapping);
                var filter = new SyncFilter<DummyPermission>(changes => changes.Select(c => c.Clone()));
                var client1Sync = SyncOperation.Create(server, client1);
                var client2Sync = SyncOperation.Create(server, client2);

                client1Sync.BatchSize = 100;
                client1Sync.Filter = filter;
                client2Sync.BatchSize = 100;
                client2Sync.Filter = filter;

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
            return new Dummy {
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
            return new DummyPermission {
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
