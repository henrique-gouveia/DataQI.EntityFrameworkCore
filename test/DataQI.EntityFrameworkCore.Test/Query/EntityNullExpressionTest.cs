using System;

using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query;
using DataQI.EntityFrameworkCore.Query.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public class EntityNullExpressionTest : IClassFixture<QueryFixture>
    {
        private readonly IEntityCommandBuilder commandBuilder;

        public EntityNullExpressionTest(QueryFixture fixture)
        {
            commandBuilder = fixture.GetCommandBuilder();
        }

        [Fact]
        public void TestRejectsNullCriterion()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new EntityNullExpression(null));
            var exceptionMessage = exception.GetBaseException().Message;

            Assert.IsType<ArgumentException>(exception.GetBaseException());
            Assert.Equal("NullExpression must not be null", exceptionMessage);
        }

        [Fact]
        public void TestBuildNullExpressionCorrectly()
        {
            var criterion = Restrictions.Null("Email");
            Assert.Equal("Email == null", criterion.GetExpressionBuilder().Build(commandBuilder));
        }
    }
}