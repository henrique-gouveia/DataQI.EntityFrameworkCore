using System;
using DataQI.Commons.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;
using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Repository.Persons
{
    public class EntityRepositoryFactoryTest : IClassFixture<DbFixture>
    {
        private readonly TestContext context;

        public EntityRepositoryFactoryTest(DbFixture fixture)
        {
            this.context = fixture.Context;
        }

        [Fact]
        public void TestRejectsNullContext()
        {
            var exception = Assert.Throws<ArgumentException>(() => 
                new EntityRepositoryFactory(null));
            var baseException = exception.GetBaseException();

            Assert.IsType<ArgumentException>(baseException);
            Assert.Equal("Context must not be null", baseException.Message);
        }

        [Fact]
        public void TestRejectsNullRepositoryInterface()
        {
            var repositoryFactory = new EntityRepositoryFactory(context);
            var entityRepository = repositoryFactory.GetRepository<IEntityRepository>();

            Assert.NotNull(entityRepository);
        }

        private interface IEntityRepository : ICrudRepository<Object, int>
        {
            
        }
    }
}