using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal static class EntityPagingBuilder<TEntity>
    {
        public static IQueryable<TEntity> BuildPagedQuery(IQueryable<TEntity> filtered, IReadOnlyList<IOrderCriterion> orders,
            IEntityType entityType, int skip, int take)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var expression = filtered.Expression;
            var first = true;
            foreach (var order in orders)
            {
                var mappedProperty = entityType?.FindProperty(order.PropertyName);
                if (mappedProperty == null)
                    throw new InvalidOperationException($"Cannot order {typeof(TEntity).Name} by unknown property '{order.PropertyName}'.");
                Expression property = mappedProperty.PropertyInfo == null
                    ? Expression.Call(typeof(EF), nameof(EF.Property), new[] { mappedProperty.ClrType }, parameter, Expression.Constant(order.PropertyName))
                    : Expression.Property(parameter, mappedProperty.PropertyInfo);
                var method = order.Direction == OrderDirection.Asc
                    ? (first ? "OrderBy" : "ThenBy") : (first ? "OrderByDescending" : "ThenByDescending");
                expression = Expression.Call(typeof(Queryable), method, new[] { typeof(TEntity), property.Type },
                    expression, Expression.Quote(Expression.Lambda(property, parameter)));
                first = false;
            }
            expression = Expression.Call(typeof(Queryable), "Skip", new[] { typeof(TEntity) }, expression,
                Expression.Property(Expression.Constant(new ValueHolder<int>(skip)), nameof(ValueHolder<int>.Value)));
            expression = Expression.Call(typeof(Queryable), "Take", new[] { typeof(TEntity) }, expression,
                Expression.Property(Expression.Constant(new ValueHolder<int>(take)), nameof(ValueHolder<int>.Value)));
            return filtered.Provider.CreateQuery<TEntity>(expression);
        }

    }
}
