using System;

using DataQI.Commons.Repository.Core;
using DataQI.Commons.Util;

namespace DataQI.EntityFrameworkCore.Repository.Support
{
    public class EntityRepositoryFactory : RepositoryFactory
    {        
        protected override object GetRepositoryInstance(Type repositoryType, params object[] args)
        {
            Assert.NotNull(repositoryType, "Repository Type must not be null");

            var repositoryMetadata = GetRepositoryMetadata(repositoryType);

            var entityRepositoryType = typeof(EntityRepository<,>);
            var repositoryInstanceType = entityRepositoryType.MakeGenericType(repositoryMetadata.EntityType, repositoryMetadata.IdType);

            var repositoryInstance = Activator.CreateInstance(repositoryInstanceType, args);
            return repositoryInstance;
        }
    }
}