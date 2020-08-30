using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DataQI.EntityFrameworkCore.Test.Repository.Persons;

namespace DataQI.EntityFrameworkCore.Test
{
    public class TestContext : DbContext
    {
        protected TestContext(DbContextOptions options) : base(options)
        {
            TryCreateDabase();
        }

        public void ClearPersons()
        {
            Persons.RemoveRange(Persons.ToList());
        }

        public static TestContext NewInstance(string dbName = "dbtest.s3db")
        {
            var connection = new SqliteConnection($"Data Source={dbName}");
            var optionsBuilder = new DbContextOptionsBuilder().UseSqlite(connection);

            var context = new TestContext(optionsBuilder.Options);

            return context;
        }

        private bool TryCreateDabase()
        {
            try
            {
                return Database.EnsureCreated();
            }
            catch
            {
                return false;
            }
        }

        public DbSet<Person> Persons { get; private set; }
    }
}