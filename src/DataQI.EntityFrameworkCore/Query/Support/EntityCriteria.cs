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

            return commandBuilder.Build();
        }
    }
}