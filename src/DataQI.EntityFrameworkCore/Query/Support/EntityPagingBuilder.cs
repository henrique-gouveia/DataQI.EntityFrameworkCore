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
        public static void EnsurePage(IPageRequest page)
        {
            if (page == null)
                throw new ArgumentException("Page must not be null", nameof(page));
            if (page.PageIndex < 0 || page.PageSize < 1 || page.PageSize == int.MaxValue)
                throw new ArgumentException("Page index must not be negative and page size must be between 1 and int.MaxValue - 1", nameof(page));
            if (page.Offset != (long)page.PageIndex * page.PageSize || page.Offset > int.MaxValue)
                throw new ArgumentException("Page offset must equal index times size and must not exceed int.MaxValue", nameof(page));
            if (page.Orders == null || page.Orders.Any(o => o == null || string.IsNullOrWhiteSpace(o.PropertyName) ||
                (o.Direction != OrderDirection.Asc && o.Direction != OrderDirection.Desc)))
                throw new ArgumentException("Page orders must contain valid property names and directions", nameof(page));
        }

        public static IReadOnlyList<IOrderCriterion> MergeOrders(IEnumerable<IOrderCriterion> criteriaOrders,
            IPageRequest page, IEnumerable<string> keyProperties)
        {
            var merged = new List<IOrderCriterion>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var order in criteriaOrders.Concat(page.Orders))
                if (seen.Add(order.PropertyName))
                    merged.Add(order);
            foreach (var key in keyProperties)
                if (seen.Add(key))
                    merged.Add(Order.Asc(key));
            if (merged.Count == 0)
                throw new InvalidOperationException($"Cannot page {typeof(TEntity).Name}: it has no primary key and no explicit ordering");
            return merged;
        }

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
                Expression property = typeof(TEntity).GetProperty(order.PropertyName) == null && key.ClrType != null
                    ? Expression.Call(typeof(EF), nameof(EF.Property), new[] { key.ClrType }, parameter, Expression.Constant(order.PropertyName))
                    : Expression.Property(parameter, order.PropertyName);
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

        public static long? DeduceTotal(int itemCount, IPageRequest page)
            => itemCount < page.PageSize && (itemCount > 0 || page.PageIndex == 0)
                ? page.Offset + itemCount : (long?)null;
    }
}
