using Microsoft.EntityFrameworkCore;
using DataQI.EntityFrameworkCore.Repository.Support;
using DataQI.EntityFrameworkCore.Test.Repository.Persons;

namespace DataQI.EntityFrameworkCore.Test.Fixtures
{
    public class DbFixture
    {
        public DbFixture()
        {
            Context = CreateTestContext();
            Context.Database.EnsureCreated();

            // 1. Default
            // PersonRepository = new PersonRepository(Context);

            // 2. Provided
            var repositoryFactory = new EntityRepositoryFactory(Context);
            PersonRepository = repositoryFactory.GetRepository<IPersonRepository>();
        }

        private TestContext CreateTestContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlite("Data Source=TestDb.sqlite3");

            var context = new TestContext(optionsBuilder.Options);
            return context;
        }

        public TestContext Context { get; }

        public IPersonRepository PersonRepository { get; }
    }
}

