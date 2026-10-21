using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataQI.EntityFrameworkCore.Test.Fixtures.Paging
{
    public sealed class ShadowKeyContext : DbContext
    {
        private readonly SqliteConnection connection;

        private ShadowKeyContext(SqliteConnection connection) : base(new DbContextOptionsBuilder().UseSqlite(connection).Options)
        {
            this.connection = connection;
        }

        public static ShadowKeyContext CreateSeeded(int rows)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var context = new ShadowKeyContext(connection);
            context.Database.EnsureCreated();
            for (var i = 0; i < rows; i++)
                context.Add(new ShadowKeyEntity { Name = "Same", Marker = i });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return context;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShadowKeyEntity>().Property<int>("ShadowId");
            modelBuilder.Entity<ShadowKeyEntity>().HasKey("ShadowId");
        }

        public override void Dispose()
        {
            base.Dispose();
            connection.Dispose();
        }
    }
}
