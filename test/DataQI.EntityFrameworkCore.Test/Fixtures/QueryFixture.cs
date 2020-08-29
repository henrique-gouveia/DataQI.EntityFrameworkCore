using System.Collections.Generic;
using System.Linq;
using DataQI.EntityFrameworkCore.Query;
using Moq;

namespace DataQI.EntityFrameworkCore.Test.Fixtures
{
    public class QueryFixture
    {
        private readonly Mock<IEntityCommandBuilder> commandBuilderMoq;
        private readonly ICollection<int> parameters;

        public QueryFixture()
        {
            commandBuilderMoq = new Mock<IEntityCommandBuilder>();
            parameters = new List<int>();

            commandBuilderMoq
                .Setup(cb => cb.AddExpressionValue(It.IsAny<object>()))
                .Returns(() =>
                {
                    var parameter = 0;
                    if (parameters.Count > 0)
                    {
                        parameter = parameters.LastOrDefault();
                        parameter++;
                    }
                    parameters.Add(parameter);

                    return parameter.ToString();
                });
        }

        public IEntityCommandBuilder GetCommandBuilder()
        {
            parameters.Clear();
            return commandBuilderMoq.Object;
        }
    }
}