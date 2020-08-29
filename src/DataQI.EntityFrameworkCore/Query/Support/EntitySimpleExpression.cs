using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Query.Extensions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntitySimpleExpression : IEntityExpressionBuilder
    {
        private readonly SimpleExpression simpleExpression;

        public EntitySimpleExpression(SimpleExpression simpleExpression)
        {
            Assert.NotNull(simpleExpression, "SimpleExpression must not be null");
            this.simpleExpression = simpleExpression;
        }

        public string Build(IEntityCommandBuilder commandBuilder)
        {
            var parameterName = commandBuilder.AddExpressionValue(simpleExpression.Value);

            return string.Format(
                simpleExpression.GetCommandTemplate(),
                simpleExpression.GetPropertyName(),
                parameterName);
        }
    }
}