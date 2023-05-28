using System;

using Xunit;

using DataQI.Commons.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;

namespace DataQI.EntityFrameworkCore.Test.Repository
{
    public sealed class EntityRepositoryFactoryTest
    {
        private readonly TestContext context;
        private readonly EntityRepositoryFactory repositoryFactory;

        public EntityRepositoryFactoryTest()
        {
            context = TestContext.NewInstance(":memory:");
            repositoryFactory = new EntityRepositoryFactory();
        }

        [Fact]
        public void TestRejectsInvalidArgs()
            => Assert.Throws<MissingMethodException>(() =>
                repositoryFactory.GetRepository<IEntityRepository>());

        [Fact]
        public void TestGetRepositoryWithArgsCorrectly()
        {
            var entityRepository = repositoryFactory.GetRepository<IEntityRepository>(context);
            Assert.NotNull(entityRepository);
        }

        [Fact]
        public void TestGetRepositoryWithRepositoryFactoryCorrectly()
        {
            var entityRepository = repositoryFactory.GetRepository<IEntityRepository>(() => 
                new EntityRepository<object, int>(context));
                
            Assert.NotNull(entityRepository);
        }

        private interface IEntityRepository : ICrudRepository<object, int>
        {

        }
    }
}