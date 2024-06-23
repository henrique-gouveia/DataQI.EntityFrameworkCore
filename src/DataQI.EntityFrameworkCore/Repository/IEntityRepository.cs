using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using DataQI.Commons.Repository;

namespace DataQI.EntityFrameworkCore.Repository
{
    public interface IEntityRepository<TEntity, TId> :
        ICrudRepository<TEntity, TId> where TEntity : class
    {
        IQueryable<TEntity> Find();
        IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);
        IEnumerable<TEntity> Find(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder);
        Task<IEnumerable<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
            CancellationToken cancellationToken = default);
    }
}