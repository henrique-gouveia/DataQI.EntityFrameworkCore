using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query.Extensions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntityCriteria : Criteria
    {
        public EntityCommand BuildCommand()
        {
            var commandBuilder = new EntityCommandBuilder();

            var criterionsEnumerator = criterions.GetEnumerator();
            while (criterionsEnumerator.MoveNext()) 
            {
                var criterion = criterionsEnumerator.Current;
                commandBuilder.AddExpression(criterion.GetExpressionBuilder());
            }

            var ordersEnumerator = orders.GetEnumerator();
            while (ordersEnumerator.MoveNext())
            {
                var order = ordersEnumerator.Current;
                commandBuilder.AddOrder(order);
            }

            return commandBuilder.Build();
        }
    }
}