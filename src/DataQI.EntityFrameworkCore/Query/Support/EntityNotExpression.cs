using System;
using System.Linq;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Query.Extensions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntityNotExpression : IEntityExpressionBuilder
    {
        private static readonly WhereOperator[] AcceptedNotOperators = new WhereOperator[] {
            WhereOperator.Between,
            WhereOperator.StartingWith,
            WhereOperator.EndingWith,
            WhereOperator.Containing,
            WhereOperator.Like,
            WhereOperator.Equal,
            WhereOperator.In,
            WhereOperator.Null
        };

        private readonly NotExpression notExpression;

        public EntityNotExpression(NotExpression notExpression)
        {
            Assert.NotNull(notExpression, "NotExpression must not be null");
            this.notExpression = notExpression;
        }

        public string Build(IEntityCommandBuilder commandBuilder)
        {
            if (!AcceptedNotOperators.Any(wo => wo == notExpression.Criterion.GetWhereOperator()))
                throw new NotImplementedException();

            var notOperator = notExpression.Criterion.GetNotOperator();
            var expression = notExpression
                .Criterion
                .GetExpressionBuilder()
                .Build(commandBuilder);

            switch (notExpression.Criterion.GetWhereOperator())
            {
                case WhereOperator.Between:
                    return $"{notOperator}({expression})";
                case WhereOperator.Equal:
                    return expression.Replace("==", "!=");
                default:
                    return $"{notOperator}{expression}";
            }
        }
    }
}