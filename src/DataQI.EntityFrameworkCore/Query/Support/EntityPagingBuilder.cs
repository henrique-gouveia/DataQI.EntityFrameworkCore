using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Repository.Paging;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal static class EntityPagingBuilder<TEntity>
    {
        public static IReadOnlyList<(string Name, Type ClrType)> KeyProperties(DbContext context)
        {
            var key = context.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey();
            return key == null ? Array.Empty<(string, Type)>() : key.Properties.Select(p => (p.Name, p.ClrType)).ToArray();
        }

        public static IQueryable<TEntity> BuildPagedQuery(IQueryable<TEntity> filtered, IReadOnlyList<IOrderCriterion> orders,
            IReadOnlyList<(string Name, Type ClrType)> keys, int skip, int take)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var expression = filtered.Expression;
            var first = true;
            foreach (var order in orders)
            {
                var key = keys.FirstOrDefault(k => k.Name == order.PropertyName);
                var clrProperty = typeof(TEntity).GetProperty(order.PropertyName);
                if (clrProperty == null && key.ClrType == null)
                    throw new InvalidOperationException($"Cannot order {typeof(TEntity).Name} by unknown property '{order.PropertyName}'.");
                Expression property = clrProperty == null
                    ? Expression.Call(typeof(EF), nameof(EF.Property), new[] { key.ClrType }, parameter, Expression.Constant(order.PropertyName))
                    : Expression.Property(parameter, clrProperty);
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
