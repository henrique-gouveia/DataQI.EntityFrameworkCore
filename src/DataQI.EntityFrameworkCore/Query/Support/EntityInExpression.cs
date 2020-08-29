using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Query.Extensions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntityInExpression : IEntityExpressionBuilder
    {
        private readonly InExpression inExpression;

        public EntityInExpression(InExpression inExpression)
        {
            Assert.NotNull(inExpression, "InExpression must not be null");
            this.inExpression = inExpression;
        }

        public string Build(IEntityCommandBuilder commandBuilder)
        {
            var parameterName = commandBuilder.AddExpressionValue(inExpression.Values);

            return string.Format(
                inExpression.GetCommandTemplate(),
                parameterName,
                inExpression.GetPropertyName());
        }
    }
}