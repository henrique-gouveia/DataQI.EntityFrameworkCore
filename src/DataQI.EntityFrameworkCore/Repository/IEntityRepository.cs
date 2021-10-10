using DataQI.Commons.Repository;

namespace DataQI.EntityFrameworkCore.Repository
{
    public interface IEntityRepository<TEntity, TId> : ICrudRepository<TEntity, TId>
        where TEntity : class
    {
         
    }
}