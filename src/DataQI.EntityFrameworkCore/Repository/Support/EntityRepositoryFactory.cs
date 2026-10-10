using System;

using DataQI.Commons.Repository.Core;
using DataQI.Commons.Util;

namespace DataQI.EntityFrameworkCore.Repository.Support
{
    /// <summary>Creates repositories backed by <see cref="EntityRepository{TEntity, TId}"/>.</summary>
    /// <remarks>
    /// The arguments passed to <c>GetRepository</c> become the constructor arguments of the repository, so the first
    /// one must be a <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
    /// </remarks>
    public class EntityRepositoryFactory : RepositoryFactory
    {        
        /// <inheritdoc />
        /// <exception cref="System.ArgumentException"><paramref name="repositoryType"/> is <c>null</c>.</exception>
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