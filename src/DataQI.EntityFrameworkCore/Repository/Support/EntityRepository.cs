using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using DataQI.Commons.Query;
using DataQI.Commons.Util;

using DataQI.EntityFrameworkCore.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;

namespace DataQI.EntityFrameworkCore.Repository.Support
{
    public class EntityRepository<TEntity, TId> : IEntityRepository<TEntity, TId>
        where TEntity : class
    {
        protected DbContext context;

        public EntityRepository(DbContext context)
        {
            Assert.NotNull(context, "DbContext must not be null");
            this.context = context;
        }

        public void Delete(TId id)
        {
            Assert.NotNull(id, "Entity Id must not be null");
            
            var entity = FindOne(id);
            context.Remove(entity);
        }

        public async Task DeleteAsync(TId id)
        {
            Assert.NotNull(id, "Entity Id must not be null");

            var entity = await FindOneAsync(id);
            await Task.FromResult(context.Remove(entity));
        }

        public bool Exists(TId id)
        {
            Assert.NotNull(id, "Id must not be null");

            var entity = FindOne(id);
            return entity != null;
        }

        public async Task<bool> ExistsAsync(TId id)
        {
            Assert.NotNull(id, "Id must not be null");

            var entity = await FindOneAsync(id);
            return entity != null;
        }

        public IEnumerable<TEntity> Find(Func<ICriteria, ICriteria> criteriaBuilder)
        {
            Assert.NotNull(criteriaBuilder, "CriteriaBuilder must not be null");

            var criteria = new EntityCriteria();
            criteriaBuilder(criteria);

            var entityCommand = criteria.BuildCommand();

            var entities = context
                .Set<TEntity>()
                .Where(entityCommand.Command, entityCommand.Values)
                .AsNoTracking()
                .ToList();

            return entities;
        }

        public async Task<IEnumerable<TEntity>> FindAsync(Func<ICriteria, ICriteria> criteriaBuilder)
        {
            Assert.NotNull(criteriaBuilder, "CriteriaBuilder must not be null");

            var criteria = new EntityCriteria();
            criteriaBuilder(criteria);

            var entityCommand = criteria.BuildCommand();

            var entities = await context
                .Set<TEntity>()
                .Where(entityCommand.Command, entityCommand.Values)
                .AsNoTracking()
                .ToListAsync();

            return entities;
        }

        public IEnumerable<TEntity> FindAll()
        {
            var entities = context
                .Set<TEntity>()
                .AsNoTracking()
                .ToList();

            return entities;
        }

        public async Task<IEnumerable<TEntity>> FindAllAsync()
        {
            var entities = await context
                .Set<TEntity>()
                .AsNoTracking()
                .ToListAsync();

            return entities;
        }

        public TEntity FindOne(TId id)
        {
            Assert.NotNull(id, "Id must not be null");

            var entity = context.Find<TEntity>(id);
            return entity;
        }
        public async Task<TEntity> FindOneAsync(TId id)
        {
            Assert.NotNull(id, "Id must not be null");

            var entity = await context.FindAsync<TEntity>(id);
            return entity;
        }

        public void Insert(TEntity entity)
        {
            Assert.NotNull(entity, "Entity must not be null");
            context.Add(entity);
        }

        public async Task InsertAsync(TEntity entity)
        {
            Assert.NotNull(entity, "Entity must not be null");
            await context.AddAsync(entity);
        }

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

        public async Task SaveAsync(TEntity entity)
        {
            Assert.NotNull(entity, "Entity must not be null");

            var entityId = context.KeyOf<TEntity, TId>(entity);
            var existingEntity = await FindOneAsync(entityId);
           
            if (existingEntity == null)
                await InsertAsync(entity);
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

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }        
}