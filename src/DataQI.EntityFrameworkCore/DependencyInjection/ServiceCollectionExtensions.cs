using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultEntityRepository<TEntity, TId, TDbContext>(this IServiceCollection services)
            where TEntity : class
            where TDbContext : DbContext
            => AddEntityRepository<IEntityRepository<TEntity, TId>, TDbContext>(services);

        public static IServiceCollection AddEntityRepository<TRepository, TDbContext>(this IServiceCollection services)
            where TRepository : class
            where TDbContext : DbContext
            => AddEntityRepository<TRepository, TDbContext>(services, null);

        public static IServiceCollection AddEntityRepository<TRepository, TRepositoryImplementation, TDbContext>(
            this IServiceCollection services)
            where TRepository : class
            where TRepositoryImplementation : class
            where TDbContext : DbContext
            => AddEntityRepository<TRepository, TDbContext>(services, typeof(TRepositoryImplementation));

        private static IServiceCollection AddEntityRepository<TRepository, TDbContext>(
            this IServiceCollection services, Type repositoryImplementationType) 
            where TRepository : class
            where TDbContext : DbContext
        {
            Assert.True(typeof(TRepository).IsInterface, "TRepository must be a repository interface.");
            Assert.True(repositoryImplementationType == null || !repositoryImplementationType.IsAbstract, 
                        "TRepositoryImplementation must be a repository concrete class");
            
            services.TryAddScoped<EntityRepositoryFactory>();
            services.TryAddScoped(serviceFactory =>
            {
                var dbContext =  serviceFactory.GetRequiredService<TDbContext>();
                var repositoryFactory = serviceFactory.GetRequiredService<EntityRepositoryFactory>();
                
                TRepository repository;
                if (repositoryImplementationType is not null)
                {
                    var repositoryImplementationInstance = Activator.CreateInstance(repositoryImplementationType, dbContext);
                    repository = repositoryFactory.GetRepository<TRepository>(() => repositoryImplementationInstance);
                }
                else
                    repository = repositoryFactory.GetRepository<TRepository>(dbContext);

                return repository;
            });

            return services;    
        }
    }
}
