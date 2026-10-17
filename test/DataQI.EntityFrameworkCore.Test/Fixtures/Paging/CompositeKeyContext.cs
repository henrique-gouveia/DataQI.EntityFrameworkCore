using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DataQI.EntityFrameworkCore.Test.Fixtures.Paging
{
    public sealed class CompositeKeyContext : DbContext
    {
        private readonly SqliteConnection connection;

        private CompositeKeyContext(SqliteConnection connection) : base(new DbContextOptionsBuilder().UseSqlite(connection).Options)
        {
            this.connection = connection;
        }

        public static CompositeKeyContext CreateSeeded(int rows)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            var context = new CompositeKeyContext(connection);
            context.Database.EnsureCreated();
            for (var i = 0; i < rows; i++)
                context.Add(new CompositeKeyEntity { Name = "Same", A = i / 2, B = i % 2 });
            context.SaveChanges();
            context.ChangeTracker.Clear();
            return context;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompositeKeyEntity>().HasKey(e => new { e.A, e.B });
        }

        public override void Dispose()
        {
            base.Dispose();
            connection.Dispose();
        }
    }
}
