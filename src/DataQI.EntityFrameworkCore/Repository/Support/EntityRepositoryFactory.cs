using System;

using Microsoft.EntityFrameworkCore;

using DataQI.Commons.Repository.Core;
using DataQI.Commons.Util;

namespace DataQI.EntityFrameworkCore.Repository.Support
{
    public class EntityRepositoryFactory : RepositoryFactory
    {
        private readonly DbContext context;

        public EntityRepositoryFactory(DbContext context)
        {
            Assert.NotNull(context, "Context must not be null");
            this.context = context;
        }

        protected override object GetCustomImplementation(Type repositoryInterface)
        {
            Assert.NotNull(repositoryInterface, "RepositoryInterface must not be null");

            var repositoryMetadata = GetRepositoryMetadata(repositoryInterface);

            var entityImplementationType = typeof(EntityRepository<,>);
            var customImplementationType = entityImplementationType.MakeGenericType(repositoryMetadata.EntityType, repositoryMetadata.IdType);
            var customImplementation = Activator.CreateInstance(customImplementationType, new[] { context });

            return customImplementation;
        }
    }
}