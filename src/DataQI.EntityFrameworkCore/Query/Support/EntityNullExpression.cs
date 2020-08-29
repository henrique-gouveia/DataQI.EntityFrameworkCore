using DataQI.Commons.Query.Support;
using DataQI.Commons.Util;
using DataQI.EntityFrameworkCore.Query.Extensions;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntityNullExpression : IEntityExpressionBuilder
    {
        private readonly NullExpression nullExpression;

        public EntityNullExpression(NullExpression nullExpression)
        {
            Assert.NotNull(nullExpression, "NullExpression must not be null");
            this.nullExpression = nullExpression;
        }
        public string Build(IEntityCommandBuilder commandBuilder)
        {
            return string.Format(
                nullExpression.GetCommandTemplate(),
                nullExpression.GetPropertyName());
        }
    }
}