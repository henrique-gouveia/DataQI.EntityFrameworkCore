using System;

using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query;
using DataQI.EntityFrameworkCore.Query.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public class EntityBetweenExpressionTest : IClassFixture<QueryFixture>
    {
        private readonly IEntityCommandBuilder commandBuilder;

        public EntityBetweenExpressionTest(QueryFixture fixture)
        {
            commandBuilder = fixture.GetCommandBuilder();
        }

        [Fact]
        public void TestRejectsNullCriterion()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new EntityBetweenExpression(null));
            var exceptionMessage = exception.GetBaseException().Message;

            Assert.IsType<ArgumentException>(exception.GetBaseException());
            Assert.Equal("BetweenExpression must not be null", exceptionMessage);
        }

        [Fact]
        public void TestBuildBetweenExpressionCorrectly()
        {
            var criterion = Restrictions.Between("DateOfBirth", DateTime.Now.AddYears(-1), DateTime.Now.AddYears(1));
            Assert.Equal("DateOfBirth >= @0 And DateOfBirth <= @1", criterion.GetExpressionBuilder().Build(commandBuilder));
        }
    }
}