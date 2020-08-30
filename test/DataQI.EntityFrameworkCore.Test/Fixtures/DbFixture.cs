using Microsoft.EntityFrameworkCore;
using DataQI.EntityFrameworkCore.Repository.Support;
using DataQI.EntityFrameworkCore.Test.Repository.Persons;
using Microsoft.Data.Sqlite;

namespace DataQI.EntityFrameworkCore.Test.Fixtures
{
    public class DbFixture
    {
        public DbFixture()
        {
            Context = TestContext.NewInstance();

            // 1. Default
            // PersonRepository = new PersonRepository(Context);

            // 2. Provided
            var repositoryFactory = new EntityRepositoryFactory(Context);
            PersonRepository = repositoryFactory.GetRepository<IPersonRepository>();
        }

        public TestContext Context { get; }

        public IPersonRepository PersonRepository { get; }
    }
}

