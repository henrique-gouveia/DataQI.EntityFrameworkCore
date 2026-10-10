using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Registers DataQI Entity Framework Core repositories in an <see cref="IServiceCollection"/>.</summary>
    /// <remarks>
    /// Registrations are scoped and use <c>TryAdd</c> semantics: existing registrations for <c>TRepository</c>
    /// and the factory are preserved, and missing registrations are added. The <c>TDbContext</c> type must be registered by the caller; the
    /// repository receives the instance resolved from the container.
    /// </remarks>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Registers <see cref="IEntityRepository{TEntity, TId}"/> for an entity type.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TId">The identifier type of <typeparamref name="TEntity"/>.</typeparam>
        /// <typeparam name="TDbContext">The context type resolved from the container.</typeparam>
        /// <param name="services">The service collection to add to.</param>
        /// <returns>The same <paramref name="services"/>, to allow chaining.</returns>
        public static IServiceCollection AddDefaultEntityRepository<TEntity, TId, TDbContext>(this IServiceCollection services)
            where TEntity : class
            where TDbContext : DbContext
            => AddEntityRepository<IEntityRepository<TEntity, TId>, TDbContext>(services);

        /// <summary>Registers a custom repository interface that the framework implements from method names.</summary>
        /// <typeparam name="TRepository">The repository interface; query methods declared on it are parsed from their names.</typeparam>
        /// <typeparam name="TDbContext">The context type resolved from the container.</typeparam>
        /// <param name="services">The service collection to add to.</param>
        /// <returns>The same <paramref name="services"/>, to allow chaining.</returns>
        /// <exception cref="System.ArgumentException"><typeparamref name="TRepository"/> is not an interface.</exception>
        public static IServiceCollection AddEntityRepository<TRepository, TDbContext>(this IServiceCollection services)
            where TRepository : class
            where TDbContext : DbContext
            => AddEntityRepository<TRepository, TDbContext>(services, null);

        /// <summary>Registers a custom repository interface served by a custom implementation.</summary>
        /// <typeparam name="TRepository">The repository interface.</typeparam>
        /// <typeparam name="TRepositoryImplementation">The concrete class the proxy forwards to; it needs a public constructor taking the context as its only argument.</typeparam>
        /// <typeparam name="TDbContext">The context type resolved from the container.</typeparam>
        /// <param name="services">The service collection to add to.</param>
        /// <returns>The same <paramref name="services"/>, to allow chaining.</returns>
        /// <exception cref="System.ArgumentException"><typeparamref name="TRepository"/> is not an interface, or <typeparamref name="TRepositoryImplementation"/> is abstract.</exception>
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
