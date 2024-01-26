namespace SimpleApi.Support;

public class UnitOfWorkRepository<TEntity, TId> : EntityRepository<TEntity, TId> where TEntity : class
{
    public IUnitOfWork UnitOfWork => (IUnitOfWork)context;
    public UnitOfWorkRepository(DbContext context) : base(context) {   }
}