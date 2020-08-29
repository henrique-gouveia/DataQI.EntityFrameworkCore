using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataQI.EntityFrameworkCore.Query.Support
{
    public class EntityCommandBuilder : IEntityCommandBuilder
    {
        private readonly ICollection<IEntityExpressionBuilder> expressions;
        private readonly IDictionary<string, object> values;

        public EntityCommandBuilder()
        {
            this.expressions = new List<IEntityExpressionBuilder>();
            this.values = new Dictionary<string, object>();
        }

        public IEntityCommandBuilder AddExpression(IEntityExpressionBuilder expression)
        {
            expressions.Add(expression);
            return this;
        }

        public string AddExpressionValue(object value)
        {
            var lastKey = values.Keys.LastOrDefault();

            var nextKey = 0;
            if (int.TryParse(lastKey, out nextKey))
                nextKey++;            

            values.Add(nextKey.ToString(), value);
            return nextKey.ToString();            
        }

        public EntityCommand Build()
        {
            var expressionBuilder = new StringBuilder();
            var expressionEnumerator = expressions.GetEnumerator();

            while (expressionEnumerator.MoveNext())
            {
                var expression = expressionEnumerator.Current;

                if (expressionBuilder.Length > 0)
                    expressionBuilder.Append(" && ");

                expressionBuilder.Append(expression.Build(this));
            }

            return new EntityCommand(expressionBuilder.ToString(), values.Values.ToArray());
        }
    }
}