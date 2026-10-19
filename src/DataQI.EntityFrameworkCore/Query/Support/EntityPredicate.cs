using System;
using System.Linq.Expressions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal sealed class EntityPredicate<TEntity>
    {
        public EntityPredicate(Expression<Func<TEntity, bool>> expression, bool isEmpty)
        {
            Expression = expression;
            IsEmpty = isEmpty;
        }

        public Expression<Func<TEntity, bool>> Expression { get; }

        public bool IsEmpty { get; }
    }
}
