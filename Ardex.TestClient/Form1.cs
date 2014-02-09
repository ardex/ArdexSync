﻿using System;
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
using Ardex.Sync.Providers;

namespace Ardex.TestClient
{
    public partial class Form1 : Form
    {
        private SyncFilter<Dummy, IChangeHistory> ExclusiveChangeHistoryFilter
        {
            get
            {
                // Change filter: emulate serialization/deserialization.
                // This is not necessary in real-world scenarios.
                return new SyncFilter<Dummy, IChangeHistory>(
                    changes => changes.Select(
                        c => new SyncEntityVersion<Dummy, IChangeHistory>(
                            c.Entity.Clone(),
                            new ChangeHistory(c.Version)
                        )
                    )
                );
            }
        }

        private SyncFilter<Dummy, ISharedChangeHistory> SharedChangeHistoryFilter
        {
            get
            {
                // Change filter: emulate serialization/deserialization.
                // This is not necessary in real-world scenarios.
                return new SyncFilter<Dummy, ISharedChangeHistory>(
                    changes => changes.Select(
                        c => new SyncEntityVersion<Dummy, ISharedChangeHistory>(
                            c.Entity.Clone(),
                            new SharedChangeHistory(c.Version)
                        )
                    )
                );
            }
        }

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
                    // --- BEGIN SYNC SETUP --- //

                    // In-memory storage.
                    var repo1 = new SyncRepository<Dummy>();
                    var repo2 = new SyncRepository<Dummy>();
                    var repo3 = new SyncRepository<Dummy>();

                    // Sync providers.
                    var changeHistory = new SyncRepository<ISharedChangeHistory>();
                    var server  = new SharedChangeHistorySyncProvider<Dummy>("Server",   "SERVER",   repo1, changeHistory, new UniqueIdMapping<Dummy>(d => d.DummyID));
                    var client1 = new SharedChangeHistorySyncProvider<Dummy>("Client 1", "CLIENT 1", repo2, changeHistory, new UniqueIdMapping<Dummy>(d => d.DummyID));
                    var client2 = new SharedChangeHistorySyncProvider<Dummy>("Client 2", "CLIENT 2", repo3, changeHistory, new UniqueIdMapping<Dummy>(d => d.DummyID));

                    server.CleanUpMetadata = false;
                    server.ConflictStrategy = SyncConflictStrategy.Winner;

                    client1.CleanUpMetadata = true;
                    client1.ConflictStrategy = SyncConflictStrategy.Loser;

                    client2.CleanUpMetadata = true;
                    client2.ConflictStrategy = SyncConflictStrategy.Loser;

                    // Chain sync operations to produce an upload/download chain.
                    //var client1Upload = SyncOperation.Create(client1, server).Filtered(this.ExclusiveChangeHistoryFilter);
                    //var client1Download = SyncOperation.Create(server, client1).Filtered(this.ExclusiveChangeHistoryFilter);
                    //var client2Upload = SyncOperation.Create(client2, server).Filtered(this.ExclusiveChangeHistoryFilter);
                    //var client2Download = SyncOperation.Create(server, client2).Filtered(this.ExclusiveChangeHistoryFilter);
                    //var client1ToClient2 = SyncOperation.Create(client1, client2).Filtered(this.ExclusiveChangeHistoryFilter);
                    //var client2ToClient1 = SyncOperation.Create(client2, client1).Filtered(this.ExclusiveChangeHistoryFilter);
                    var client1Upload = SyncOperation.Create(client1, server).Filtered(this.SharedChangeHistoryFilter);
                    var client1Download = SyncOperation.Create(server, client1).Filtered(this.SharedChangeHistoryFilter);
                    var client2Upload = SyncOperation.Create(client2, server).Filtered(this.SharedChangeHistoryFilter);
                    var client2Download = SyncOperation.Create(server, client2).Filtered(this.SharedChangeHistoryFilter);
                    var client1ToClient2 = SyncOperation.Create(client1, client2).Filtered(this.SharedChangeHistoryFilter);
                    var client2ToClient1 = SyncOperation.Create(client2, client1).Filtered(this.SharedChangeHistoryFilter);

                    // Chain uploads and downloads to produce complete sync sessions.
                    var client1Sync = SyncOperation.Chain(client1Upload, client1Download);
                    var client2Sync = SyncOperation.Chain(client2Upload, client2Download);

                    // --- END SYNC SETUP --- //

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
                            //var repo1Dummy1 = repo1.Single(d => d.DummyID == dummy1.DummyID);

                            //repo1Dummy1.Text = "First dummy upd repo 1";

                            //repo1.Update(repo1Dummy1);

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
                                    repo3.Lock.EnterWriteLock();

                                    try
                                    {
                                        var repo3Dummy3 = repo3.Single(d => d.DummyID == dummy2.DummyID);

                                        repo3Dummy3.Text = "Dodgy concurrent update";

                                        repo3.Update(repo3Dummy3);
                                    }
                                    finally
                                    {
                                        repo3.Lock.ExitWriteLock();
                                    }
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

                var ownerIdMapping = new UniqueIdMapping<DummyPermission>(d => d.SourceReplicaID);

                var server = new SimpleRepositorySyncProvider<DummyPermission, Timestamp>("Server", repo1, uniqueIdMapping, timestampMapping, comparer, ownerIdMapping);
                var client1 = new SimpleRepositorySyncProvider<DummyPermission, Timestamp>("Client 1", repo2, uniqueIdMapping, timestampMapping, comparer, ownerIdMapping);
                var client2 = new SimpleRepositorySyncProvider<DummyPermission, Timestamp>("Client 2", repo3, uniqueIdMapping, timestampMapping, comparer, ownerIdMapping);
                var filter = new SyncFilter<DummyPermission, Timestamp>(changes => changes.Select(c => SyncEntityVersion.Create(c.Entity.Clone(), new Timestamp(c.Version.ToLong()))));
                
                // Sync ops.
                var client1Upload = SyncOperation.Create(client1, server).Filtered(filter);
                var client1Download = SyncOperation.Create(server, client1).Filtered(filter);
                var client2Upload = SyncOperation.Create(client2, server).Filtered(filter);
                var client2Download = SyncOperation.Create(server, client2).Filtered(filter);

                var client1Sync = SyncOperation.Chain(client1Upload, client1Download);
                var client2Sync = SyncOperation.Chain(client2Upload, client2Download);

                var nextTimestamp = new Func<SyncProvider<DummyPermission, Timestamp>, Timestamp>(provider =>
                {
                    var maxTimestamp = provider.Repository
                        .Where(d => d.SourceReplicaID == provider.ReplicaID)
                        .Select(d => d.Timestamp)
                        .DefaultIfEmpty()
                        .Max();

                    return maxTimestamp == null ? new Timestamp(1) : ++maxTimestamp;
                });

                // Begin.
                var permission1 = new DummyPermission { DummyPermissionID = Guid.NewGuid(), Timestamp = nextTimestamp(server), SourceReplicaID = server.ReplicaID };
                {
                    // Legal.
                    repo1.Insert(permission1);

                    await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                }

                var permission2 = new DummyPermission { DummyPermissionID = Guid.NewGuid(), Timestamp = nextTimestamp(client1), SourceReplicaID = client1.ReplicaID };
                {
                    // Legal.
                    repo2.Insert(permission2);

                    await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                    await Task.WhenAll(client1Sync.SynchroniseDiffAsync(), client2Sync.SynchroniseDiffAsync());
                }

                {
                    Debug.Print("SERVER");
                    Debug.Print(repo1.ToString());
                    Debug.Print("CLIENT 1");
                    Debug.Print(repo2.ToString());
                    Debug.Print("CLIENT 2");
                    Debug.Print(repo3.ToString());

                    // Illegal.
                    var repo2Permission1 = repo2.Single(p => p.DummyPermissionID == permission1.DummyPermissionID);

                    repo2Permission1.SourceReplicaID = client1.ReplicaID;
                    repo2Permission1.Timestamp = nextTimestamp(client1);

                    repo2.Update(repo2Permission1);

                    Debug.Print("SERVER");
                    Debug.Print(repo1.ToString());
                    Debug.Print("CLIENT 1");
                    Debug.Print(repo2.ToString());
                    Debug.Print("CLIENT 2");
                    Debug.Print(repo3.ToString());

                    var anchor = client1.LastAnchor();

                    await client1Sync.SynchroniseDiffAsync();
                    await client2Sync.SynchroniseDiffAsync();
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

        public class FileAccessInfo
        {
            public string FileName { get; set; }
            public DateTime LastModified { get; set; }

            //public void WriteTo(string directoryPath)
            //{
            //    var filePath = Path.Combine(directoryPath, this.FileName);

            //    for (var i = 0; i < 10; i++)
            //    {
            //        try
            //        {
            //            using (var s = this.OpenRead())
            //            {
            //                this.GetContentFunc().CopyTo(s);
            //            }

            //            return;
            //        }
            //        catch (IOException)
            //        {

            //        }
            //    }
            //}

            //public Stream OpenRead()
            //{
            //    for (var i = 0; i < 10; i++)
            //    {
            //        try
            //        {
            //            return this.GetContentFunc();
            //        }
            //        catch (IOException)
            //        {

            //        }
            //    }

            //    throw new IOException();
            //}
        }

        //public SyncRepository<FileAccessInfo> FileRepo(string directoryPath)
        //{
        //    var repo = new SyncRepository<FileAccessInfo>();

        //    repo.EntityInserted += e => 
        //    repo.EntityUpdated += e => e.WriteTo(directoryPath);
        //    repo.EntityDeleted += e => e.WriteTo(directoryPath);

        //    var watcher = new FileSystemWatcher(directoryPath);

        //    watcher.Created += (s, e) =>
        //    {
        //        // See if we already have that entry.
        //        var getContentsFunc = new Func<Stream>(() => this.OpenRead(e.FullPath));

        //        using (var crypto = System.Security.Cryptography.SHA1.Create())
        //        {
        //            var hash = crypto.ComputeHash(getContentsFunc());

        //            // See if file entry already exists.
        //            var fai = repo.FirstOrDefault(f => f.FileName == e.Name);

        //            if (fai != null)
        //            {
        //                // Update only if necessary.
        //                if (fai.Checksum != hash)
        //                {
        //                    fai.Checksum = hash;
        //                    fai.GetContentFunc = getContentsFunc;

        //                    repo.Update(fai);
        //                }
        //            }
        //            else
        //            {
        //                fai = new FileAccessInfo {
        //                    FileName = e.Name,
        //                    Checksum = hash,
        //                    GetContentFunc = getContentsFunc
        //                };

        //                repo.Insert(fai);
        //            }
        //        }
        //    };

        //    watcher.EnableRaisingEvents = true;

        //    return repo;
        //}

        private async void button3_Click(object sender, EventArgs e)
        {
            //this.button3.Enabled = false;

            //try
            //{
            //    var path1 = @"C:\dev\DirectorySyncTest\Dir 1";
            //    var path2 = @"C:\dev\DirectorySyncTest\Dir 2";

            //    var repo1 = new SyncRepository<FileAccessInfo>();
            //    var repo2 = new SyncRepository<FileAccessInfo>();

            //    repo1.EntityInserted += f => File.WriteAllBytes()

            //    var provider1 = new ExclusiveChangeHistorySyncProvider<FileAccessInfo>("Dir 1", repo1, new SyncRepository<IChangeHistory>(), new UniqueIdMapping<FileAccessInfo>(f => f.FileName));
            //    var provider2 = new ExclusiveChangeHistorySyncProvider<FileAccessInfo>("Dir 2", repo2, new SyncRepository<IChangeHistory>(), new UniqueIdMapping<FileAccessInfo>(f => f.FileName));

            //    var sync1 = SyncOperation.Create(provider1, provider2);
            //    var sync2 = SyncOperation.Create(provider2, provider1);
            //    var sync = SyncOperation.Chain(sync1, sync2);

            //    while (true)
            //    {
            //        await Task.Delay(TimeSpan.FromSeconds(5));
            //        await sync.SynchroniseDiffAsync();
            //    }
            //}
            //finally
            //{
            //    this.button3.Enabled = true;
            //}

            //this.button3.Enabled = false;

            //try
            //{
            //    var dir1 = new FolderRepository(@"C:\dev\DirectorySyncTest\Dir 1");
            //    var dir2 = new FolderRepository(@"C:\dev\DirectorySyncTest\Dir 2");

            //    var changeHistory1 = new SyncRepository<IChangeHistory>();
            //    var changeHistory2 = new SyncRepository<IChangeHistory>();

            //    var tracking1 = new ChangeTrackingFactory("Dir 1").Exclusive(dir1, changeHistory1, new UniqueIdMapping<FileEntry>(f => f.FileName));
            //    var tracking2 = new ChangeTrackingFactory("Dir 2").Exclusive(dir2, changeHistory2, new UniqueIdMapping<FileEntry>(f => f.FileName));

            //    var provider1 = MergeSyncProvider.Create(tracking1);
            //    var provider2 = MergeSyncProvider.Create(tracking2);

            //    provider2.CleanUpMetadataAfterSync = true;

            //    var stage1 = SyncOperation.Create(provider1, provider2);
            //    var stage2 = SyncOperation.Create(provider2, provider1);
            //    var sync = SyncOperation.Chain(stage1, stage2);

            //    MessageBox.Show("Starting sync");

            //    while (true)
            //    {
            //        await Task.Delay(TimeSpan.FromSeconds(5));
            //        await sync.SynchroniseDiffAsync();
            //    }
            //}
            //finally
            //{
            //    this.button3.Enabled = true;
            //}
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
        public Guid DummyPermissionID { get; set; }
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
            return this.DummyPermissionID.GetHashCode();
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
