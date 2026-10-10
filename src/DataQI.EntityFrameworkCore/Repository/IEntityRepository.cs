using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using DataQI.Commons.Repository;

namespace DataQI.EntityFrameworkCore.Repository
{
    /// <summary>Defines the Entity Framework Core repository of an entity type.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The identifier type of <typeparamref name="TEntity"/>.</typeparam>
    /// <remarks>In addition to the members of <see cref="ICrudRepository{TEntity, TId}"/>, it exposes LINQ based searches.</remarks>
    public interface IEntityRepository<TEntity, in TId> :
        ICrudRepository<TEntity, TId> where TEntity : class
    {
        /// <summary>Gets a queryable over all entities for composing further LINQ operators.</summary>
        /// <returns>A deferred query using the context's default tracking behavior; nothing runs until it is enumerated.</returns>
        IQueryable<TEntity> Find();
        /// <summary>Finds the entities that satisfy a predicate.</summary>
        /// <param name="predicate">The filter expression; must not be <c>null</c>.</param>
        /// <returns>The matching entities, not tracked by the context.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="predicate"/> is <c>null</c>.</exception>
        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        /// <summary>Finds the entities that satisfy a predicate asynchronously.</summary>
        /// <param name="predicate">The filter expression; must not be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to observe while waiting for the task to complete.</param>
        /// <returns>A task that yields the matching entities, not tracked by the context.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="predicate"/> is <c>null</c>.</exception>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);
        /// <summary>Finds the entities returned by a query composed over the entity set.</summary>
        /// <param name="queryBuilder">A function that receives the queryable entity set and returns the query to run; must not be <c>null</c>.</param>
        /// <returns>The query results, tracked according to the query and the context's default behavior.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="queryBuilder"/> is <c>null</c>.</exception>
        IEnumerable<TEntity> Find(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder);
        /// <summary>Finds the entities returned by a query composed over the entity set asynchronously.</summary>
        /// <param name="queryBuilder">A function that receives the queryable entity set and returns the query to run; must not be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to observe while waiting for the task to complete.</param>
        /// <returns>A task that yields the query results, tracked according to the query and the context's default behavior.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="queryBuilder"/> is <c>null</c>.</exception>
        Task<IEnumerable<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
            CancellationToken cancellationToken = default);
    }
}