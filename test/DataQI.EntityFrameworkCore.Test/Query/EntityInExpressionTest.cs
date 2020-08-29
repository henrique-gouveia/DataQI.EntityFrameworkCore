using System;

using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query;
using DataQI.EntityFrameworkCore.Query.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public class EntityInExpressionTest : IClassFixture<QueryFixture>
    {
        private readonly IEntityCommandBuilder commandBuilder;

        public EntityInExpressionTest(QueryFixture fixture)
        {
            commandBuilder = fixture.GetCommandBuilder();
        }

        [Fact]
        public void TestRejectsNullCriterion()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new EntityInExpression(null));
            var exceptionMessage = exception.GetBaseException().Message;

            Assert.IsType<ArgumentException>(exception.GetBaseException());
            Assert.Equal("InExpression must not be null", exceptionMessage);
        }

        [Fact]
        public void TestBuildInExpressionCorrectly()
        {
            var criterion = Restrictions.In("FirstName", new string[] { "Fake Name A", "Fake Name B", "Fake Name C" });
            Assert.Equal("@0.Contains(FirstName)", criterion.GetExpressionBuilder().Build(commandBuilder));
        }
    }
}