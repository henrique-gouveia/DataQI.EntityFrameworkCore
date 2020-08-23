using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Query.Extensions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntityBetweenExpression : IEntityExpressionBuilder
    {
        private readonly BetweenExpression betweenExpression;

        public EntityBetweenExpression(BetweenExpression betweenExpression)
        {
            Assert.NotNull(betweenExpression, "BetweenExpression must not be null");
            this.betweenExpression = betweenExpression;
        }

        public string Build(IEntityCommandBuilder commandBuilder)
        {
            var parameterNameStarts = commandBuilder.AddExpressionValue(betweenExpression.Starts);
            var parameterNameEnds = commandBuilder.AddExpressionValue(betweenExpression.Ends);

            return string.Format(
                betweenExpression.GetCommandTemplate(),
                betweenExpression.GetPropertyName(),
                parameterNameStarts,
                parameterNameEnds);
        }
    }
}