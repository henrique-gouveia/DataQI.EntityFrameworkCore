using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    internal static class EntityOrderedQueryBuilder<TEntity>
    {
        public static IQueryable<TEntity> BuildOrderedQuery(
            IQueryable<TEntity> query, IReadOnlyCollection<IOrderCriterion> orders)
        {
            if (orders.Count == 0)
                return query;

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var expression = query.Expression;
            var isFirst = true;

            foreach (var order in orders)
            {
                var property = Expression.Property(parameter, order.PropertyName);
                var methodName = order.Direction == OrderDirection.Asc
                    ? (isFirst ? "OrderBy" : "ThenBy")
                    : (isFirst ? "OrderByDescending" : "ThenByDescending");

                expression = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new[] { typeof(TEntity), property.Type },
                    expression,
                    Expression.Quote(Expression.Lambda(property, parameter)));

                isFirst = false;
            }

            return query.Provider.CreateQuery<TEntity>(expression);
        }
    }
}
