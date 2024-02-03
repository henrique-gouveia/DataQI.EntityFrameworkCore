using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using DataQI.EntityFrameworkCore.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;

namespace DataQI.EntityFrameworkCore.Test.DependencyInjection
{
    public class ServiceColletionExtensionsTest
    {
        private readonly IServiceCollection services = new ServiceCollection().AddSingleton<EntityContext>();

        [Fact]
        public void TestRejectsInvalidRepositoryType()
        {
            var exception = Assert.Throws<ArgumentException>(() => 
                services.AddEntityRepository<EntityRepository, EntityContext>());
            Assert.Equal("TRepository must be a repository interface.", exception.Message);
        }

        [Fact]
        public void TestRejectsInvalidRepositoryImplementationType()
        {
            var exception = Assert.Throws<ArgumentException>(() => 
                services.AddEntityRepository<IEntityRepository, IEntityRepository, EntityContext>()        );
            Assert.Equal("TRepositoryImplementation must be a repository concrete class", exception.Message);
        }
        
        [Fact]
        public void TestAddDefaultEntityRepository()
        {
            services.AddDefaultEntityRepository<Entity, int, EntityContext>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<IEntityRepository<Entity, int>>();

            Assert.NotNull(repository);
        }

        [Fact]
        public void TestAddEntityRepository()
        {
            services.AddEntityRepository<IEntityRepository, EntityContext>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<IEntityRepository>();

            Assert.NotNull(repository);
        }

        [Fact]
        public void TestAddEntityRepositoryImplementation()
        {
            services.AddEntityRepository<IEntityRepository, EntityRepository, EntityContext>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<IEntityRepository>();
            
            Assert.NotNull(repository);
        }
        
        [Fact]
        public void TestAddSimpleEntityRepositoryMultipleTimes()
        {
            services.AddDefaultEntityRepository<Entity, int, EntityContext>();
            services.AddDefaultEntityRepository<Entity, int, EntityContext>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<IEntityRepository<Entity, int>>();

            Assert.Equal(1, services.Count(sd => sd.ServiceType == typeof(IEntityRepository<Entity, int>)));
            Assert.Equal(1, services.Count(sd => sd.ServiceType == typeof(EntityRepositoryFactory)));
            Assert.NotNull(repository);
        }

        [Fact]
        public void TestAddEntityRepositoryMultipleTimes()
        {
            services.AddEntityRepository<IEntityRepository, EntityContext>();
            services.AddEntityRepository<IEntityRepository, EntityContext>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<IEntityRepository>();

            Assert.Equal(1, services.Count(sd => sd.ServiceType == typeof(IEntityRepository)));
            Assert.Equal(1, services.Count(sd => sd.ServiceType == typeof(EntityRepositoryFactory)));
            Assert.NotNull(repository);
        }
        
        [Fact]
        public void TestAddEntityRepositoryImplementationMultipleTimes()
        {
            services.AddEntityRepository<IEntityRepository, EntityRepository, EntityContext>();
            services.AddEntityRepository<IEntityRepository, EntityRepository, EntityContext>();

            var serviceProvider = services.BuildServiceProvider();
            var repository = serviceProvider.GetService<IEntityRepository>();

            Assert.Equal(1, services.Count(sd => sd.ServiceType == typeof(IEntityRepository)));
            Assert.Equal(1, services.Count(sd => sd.ServiceType == typeof(EntityRepositoryFactory)));
            Assert.NotNull(repository);
        }

        private interface IEntityRepository : IEntityRepository<Entity, int> {  }

        private class EntityRepository : EntityRepository<Entity, int>, IEntityRepository
        {
            public EntityRepository(DbContext context) : base(context) { }
        }

        public sealed class Entity
        {
            public int Id { get; set; }
        }

        public sealed class EntityContext : DbContext
        {
            public DbSet<Entity> Entities { get; set; } = null!;
        }
    }
}
