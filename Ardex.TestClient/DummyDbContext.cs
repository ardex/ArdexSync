using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;

using Ardex.Sync.ChangeTracking;

namespace Ardex.TestClient
{
    public abstract class DummyDbContext : DbContext
    {
        public DbSet<Dummy> Dummies { get; set; }
        public DbSet<ChangeHistory> ChangeHistory { get; set; }

        protected DummyDbContext(string connectionString) : base(new SqlConnection(connectionString), true) { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dummy>()
                        .HasKey(d => d.EntityGuid);

            modelBuilder.Entity<Dummy>()
                        .Property(d => d.EntityGuid)
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
