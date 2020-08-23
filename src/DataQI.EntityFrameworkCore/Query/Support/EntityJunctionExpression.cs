using System.Collections.Generic;
using System.Text;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;

using DataQI.EntityFrameworkCore.Query.Extensions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntityJunctionExpression : IEntityExpressionBuilder
    {
        private readonly Junction junction;

        public EntityJunctionExpression(Junction junction)
        {
            Assert.NotNull(junction, "Junction must not be null");
            this.junction = junction;
        }
        public string Build(IEntityCommandBuilder commandBuilder)
        {
            var sqlWhereBuilder = new StringBuilder();
            var expressionsNumerator = BuildExpressionsBuilder().GetEnumerator();

            while (expressionsNumerator.MoveNext())
            {
                if (sqlWhereBuilder.Length > 0)
                    sqlWhereBuilder.Append(junction.GetLogicalOperator());

                var expressionBuilder = expressionsNumerator.Current;
                sqlWhereBuilder.Append(expressionBuilder.Build(commandBuilder));
            }

            return string.Format(
                junction.GetCommandTemplate(),
                sqlWhereBuilder.ToString());
        }

        private IReadOnlyCollection<IEntityExpressionBuilder> BuildExpressionsBuilder()
        {
            var criterionsEnumerator = junction.Criterions.GetEnumerator();
            var expressions = new List<IEntityExpressionBuilder>();

            while (criterionsEnumerator.MoveNext())
            {
                var criterion = criterionsEnumerator.Current;

                IEntityExpressionBuilder builder = criterion.GetExpressionBuilder();
                expressions.Add(builder);
            }

            return expressions;
        }
    }
}