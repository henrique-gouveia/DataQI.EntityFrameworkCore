using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;

using DataQI.EntityFrameworkCore.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;

namespace DataQI.EntityFrameworkCore.Repository.Support
{
    /// <summary>Implements <see cref="IEntityRepository{TEntity, TId}"/> on top of a <see cref="DbContext"/>.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The type of the entity's single key property.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="Insert(TEntity)"/>, <see cref="Save(TEntity)"/> and <see cref="Delete(TId)"/> (and their async
    /// versions) only register the change in the context's change tracker. Nothing reaches the database until the
    /// caller calls <c>SaveChanges</c> or <c>SaveChangesAsync</c> on the context.
    /// </para>
    /// <para>
    /// Criteria and predicate searches, including <c>FindAll</c>, return entities without tracking.
    /// <c>Find()</c> and query builders start with the context's default tracking behavior;
    /// a query builder can change it with LINQ operators. Identifier lookups return tracked entities.
    /// </para>
    /// <para>Disposing the repository disposes the context it was given.</para>
    /// </remarks>
    public class EntityRepository<TEntity, TId> :
        IEntityRepository<TEntity, TId> where TEntity : class
    {
        /// <summary>Holds the context used by every operation, which is disposed and set to <c>null</c> when the repository is disposed.</summary>
        protected DbContext context;

        /// <summary>Initializes a new instance of the <see cref="EntityRepository{TEntity, TId}"/> class.</summary>
        /// <param name="context">The context to work with; must not be <c>null</c>.</param>
        /// <exception cref="System.ArgumentException"><paramref name="context"/> is <c>null</c>.</exception>
        public EntityRepository(DbContext context)
        {
            Assert.NotNull(context, "DbContext must not be null");
            this.context = context;
        }

        /// <summary>Marks the entity with the given identifier for deletion; call <c>SaveChanges</c> on the context to apply it.</summary>
        /// <param name="id">The identifier of an existing entity; must not be <c>null</c>.</param>
        /// <exception cref="System.ArgumentException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <remarks>Deleting an identifier for which no entity exists is not supported.</remarks>
        public void Delete(TId id)
        {
            Assert.NotNull(id, "Entity Id must not be null");
            var entity = FindOne(id);
            context.Remove(entity);
        }

        /// <summary>Marks the entity with the given identifier for deletion asynchronously; call <c>SaveChangesAsync</c> on the context to apply it.</summary>
        /// <param name="id">The identifier of an existing entity; must not be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to observe while looking the entity up.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <remarks>Deleting an identifier for which no entity exists is not supported.</remarks>
        public async Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
        {
            Assert.NotNull(id, "Entity Id must not be null");
            var entity = await FindOneAsync(id, cancellationToken);
            await Task.FromResult(context.Remove(entity));
        }

        /// <summary>Determines whether an entity with the given identifier exists.</summary>
        /// <param name="id">The identifier to look for; must not be <c>null</c>.</param>
        /// <returns><c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="id"/> is <c>null</c>.</exception>
        public bool Exists(TId id)
        {
            Assert.NotNull(id, "Id must not be null");
            var entity = FindOne(id);
            return entity != null;
        }

        /// <summary>Determines asynchronously whether an entity with the given identifier exists.</summary>
        /// <param name="id">The identifier to look for; must not be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to observe while waiting for the task to complete.</param>
        /// <returns>A task that yields <c>true</c> if the entity exists; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="id"/> is <c>null</c>.</exception>
        public async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
        {
            Assert.NotNull(id, "Id must not be null");
            var entity = await FindOneAsync(id, cancellationToken);
            return entity != null;
        }
        
        /// <inheritdoc />
        public IQueryable<TEntity> Find() => context.Set<TEntity>().AsQueryable();

        /// <inheritdoc />
        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            Assert.NotNull(predicate, "Predicate must not be null");
            var entities = context
                .Set<TEntity>()
                .AsNoTracking()
                .Where(predicate)
                .ToList();
            return entities;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            Assert.NotNull(predicate, "Predicate must not be null");
            var entities = await context
                .Set<TEntity>()
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync(cancellationToken);
            return entities;
        }

        /// <inheritdoc />
        public IEnumerable<TEntity> Find(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder)
        {
            Assert.NotNull(queryBuilder, "QueryBuilder must not be null");
            var query = context.Set<TEntity>().AsQueryable();
            query = queryBuilder(query);
            var entities = query.ToList();
            return entities;
        }
        
                
        /// <inheritdoc />
        public async Task<IEnumerable<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
            CancellationToken cancellationToken = default)
        {
            Assert.NotNull(queryBuilder, "QueryBuilder must not be null");
            var query = context.Set<TEntity>().AsQueryable();
            query = queryBuilder(query);
            var entities = await query.ToListAsync(cancellationToken);
            return entities;
        }

        /// <inheritdoc />
        /// <remarks>The returned entities are not tracked by the context.</remarks>
        public IEnumerable<TEntity> Find(Func<ICriteria, ICriteria> criteriaBuilder)
            => CriteriaQuery(criteriaBuilder).ToList();

        /// <inheritdoc />
        /// <remarks>The returned entities are not tracked by the context.</remarks>
        public async Task<IEnumerable<TEntity>> FindAsync(Func<ICriteria, ICriteria> criteriaBuilder,
            CancellationToken cancellationToken = default)
            => await CriteriaQuery(criteriaBuilder).ToListAsync(cancellationToken);

        /// <inheritdoc />
        /// <remarks>The returned entities are not tracked by the context.</remarks>
        public TEntity FindOne(Func<ICriteria, ICriteria> criteriaBuilder)
            => CriteriaQuery(criteriaBuilder).SingleOrDefault();

        /// <inheritdoc />
        /// <remarks>The returned entities are not tracked by the context.</remarks>
        public Task<TEntity> FindOneAsync(Func<ICriteria, ICriteria> criteriaBuilder,
            CancellationToken cancellationToken = default)
            => CriteriaQuery(criteriaBuilder).SingleOrDefaultAsync(cancellationToken);

        private IQueryable<TEntity> CriteriaQuery(Func<ICriteria, ICriteria> criteriaBuilder)
        {
            Assert.NotNull(criteriaBuilder, "CriteriaBuilder must not be null");
            var criteria = criteriaBuilder(new Criteria());

            var predicate = EntityPredicateVisitor<TEntity>.BuildPredicate(criteria);
            var query = context.Set<TEntity>().AsNoTracking().Where(predicate);

            return EntityOrderedQueryBuilder<TEntity>.BuildOrderedQuery(query, criteria.Orders);
        }

        /// <inheritdoc />
        /// <remarks>The returned entities are not tracked by the context.</remarks>
        public IEnumerable<TEntity> FindAll()
        {
            var entities = context
                .Set<TEntity>()
                .AsNoTracking()
                .ToList();
            return entities;
        }

        /// <inheritdoc />
        /// <remarks>The returned entities are not tracked by the context.</remarks>
        public async Task<IEnumerable<TEntity>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await context
                .Set<TEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return entities;
        }

        /// <summary>Finds the entity with the given key value.</summary>
        /// <param name="id">The value of the entity's single key property; must not be <c>null</c>.</param>
        /// <returns>The entity, or <c>null</c> when it does not exist. An entity already tracked by the context is returned without querying the database; the result is tracked.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <remarks>Composite keys are not supported by this method.</remarks>
        public TEntity FindOne(TId id)
        {
            Assert.NotNull(id, "Id must not be null");
            var entity = context.Find<TEntity>(id);
            return entity;
        }
        /// <summary>Finds the entity with the given key value asynchronously.</summary>
        /// <param name="id">The value of the entity's single key property; must not be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to observe while waiting for the task to complete.</param>
        /// <returns>A task that yields the entity, or <c>null</c> when it does not exist. An entity already tracked by the context is returned without querying the database; the result is tracked.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="id"/> is <c>null</c>.</exception>
        /// <remarks>Composite keys are not supported by this method.</remarks>
        public async Task<TEntity> FindOneAsync(TId id, CancellationToken cancellationToken = default)
        {
            Assert.NotNull(id, "Id must not be null");
            var entity = await context.FindAsync<TEntity>(
                keyValues: new object[] { id }, 
                cancellationToken: cancellationToken);
            return entity;
        }

        /// <summary>Adds the entity to the context as new; call <c>SaveChanges</c> on the context to insert it.</summary>
        /// <param name="entity">The entity to add; must not be <c>null</c>.</param>
        /// <exception cref="System.ArgumentException"><paramref name="entity"/> is <c>null</c>.</exception>
        public void Insert(TEntity entity)
        {
            Assert.NotNull(entity, "Entity must not be null");
            context.Add(entity);
        }

        /// <summary>Adds the entity to the context asynchronously as new; call <c>SaveChangesAsync</c> on the context to insert it.</summary>
        /// <param name="entity">The entity to add; must not be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="entity"/> is <c>null</c>.</exception>
        public async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            Assert.NotNull(entity, "Entity must not be null");
            await context.AddAsync(entity, cancellationToken);
        }

        /// <summary>Adds the entity when none with the same key exists; otherwise copies its values onto the existing entity and marks it modified; call <c>SaveChanges</c> on the context to apply it.</summary>
        /// <param name="entity">The entity to save; must not be <c>null</c>.</param>
        /// <exception cref="System.ArgumentException"><paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The entity's primary key is composite.</exception>
        public void Save(TEntity entity)
        {
            Assert.NotNull(entity, "Entity must not be null");

            var entityId = context.KeyOf<TEntity, TId>(entity);
            var existingEntity = FindOne(entityId);
            if (existingEntity == null)
                Insert(entity);
            else
                ChangeExistingEntityValues(existingEntity, entity);
        }

        /// <summary>Adds the entity asynchronously when none with the same key exists; otherwise copies its values onto the existing entity and marks it modified; call <c>SaveChangesAsync</c> on the context to apply it.</summary>
        /// <param name="entity">The entity to save; must not be <c>null</c>.</param>
        /// <param name="cancellationToken">The token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="entity"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The entity's primary key is composite.</exception>
        public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            Assert.NotNull(entity, "Entity must not be null");

            var entityId = context.KeyOf<TEntity, TId>(entity);
            var existingEntity = await FindOneAsync(entityId, cancellationToken);
            if (existingEntity == null)
                await InsertAsync(entity, cancellationToken);
            else
                ChangeExistingEntityValues(existingEntity, entity);
        }

        private void ChangeExistingEntityValues(TEntity existing, TEntity entity)
        {
            var entityEntry = context.Entry(existing);
            entityEntry.CurrentValues.SetValues(entity);
            entityEntry.State = EntityState.Modified;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        /// <summary>Releases the context when <paramref name="disposing"/> is <c>true</c>; later calls do nothing.</summary>
        /// <param name="disposing"><c>true</c> when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    context?.Dispose();
                    context = null;
                }
                disposedValue = true;
            }
        }

        /// <summary>Releases the context used by this repository.</summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }        
}
