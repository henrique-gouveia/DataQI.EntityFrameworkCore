using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal static class EntityExpressionBuilder<TEntity> where TEntity : class
    {
        public static IQueryable<TEntity> BuildFind(IQueryable<TEntity> source, EntityPredicate<TEntity> filter,
            IReadOnlyList<IOrderCriterion> orders, IEntityType entityType)
            => ApplyOrders(BuildWhere(source, filter), orders, entityType);

        public static IQueryable<TEntity> BuildPage(IQueryable<TEntity> source, EntityPredicate<TEntity> filter,
            IReadOnlyList<IOrderCriterion> orders, IEntityType entityType, long skip, long take)
        {
            var ordered = ApplyOrders(BuildWhere(source, filter), orders, entityType);
            var expression = Expression.Call(typeof(Queryable), "Skip", new[] { typeof(TEntity) }, ordered.Expression,
                Expression.Property(Expression.Constant(new ValueHolder<int>((int)skip)), nameof(ValueHolder<int>.Value)));
            expression = Expression.Call(typeof(Queryable), "Take", new[] { typeof(TEntity) }, expression,
                Expression.Property(Expression.Constant(new ValueHolder<int>((int)take)), nameof(ValueHolder<int>.Value)));
            return ordered.Provider.CreateQuery<TEntity>(expression);
        }

        public static IQueryable<TEntity> BuildWhere(IQueryable<TEntity> source, EntityPredicate<TEntity> filter)
            => filter.IsEmpty ? source : source.Where(filter.Expression);

        private static IQueryable<TEntity> ApplyOrders(IQueryable<TEntity> query,
            IReadOnlyList<IOrderCriterion> orders, IEntityType entityType)
        {
            if (orders.Count == 0)
                return query;

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var expression = query.Expression;
            var first = true;
            foreach (var order in orders)
            {
                var mappedProperty = entityType.FindProperty(order.PropertyName);
                Assert.NotNull<InvalidOperationException>(mappedProperty,
                    () => $"Cannot order {typeof(TEntity).Name} by unknown property '{order.PropertyName}'.");

                Expression property = mappedProperty.PropertyInfo == null
                    ? Expression.Call(typeof(EF), nameof(EF.Property), new[] { mappedProperty.ClrType },
                        parameter, Expression.Constant(order.PropertyName))
                    : Expression.Property(parameter, mappedProperty.PropertyInfo);
                var method = order.Direction == OrderDirection.Asc
                    ? (first ? "OrderBy" : "ThenBy")
                    : (first ? "OrderByDescending" : "ThenByDescending");
                expression = Expression.Call(typeof(Queryable), method,
                    new[] { typeof(TEntity), property.Type }, expression,
                    Expression.Quote(Expression.Lambda(property, parameter)));
                first = false;
            }
            return query.Provider.CreateQuery<TEntity>(expression);
        }
    }
}
