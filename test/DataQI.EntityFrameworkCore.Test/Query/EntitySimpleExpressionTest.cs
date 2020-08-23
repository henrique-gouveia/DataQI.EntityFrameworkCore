using System;

using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query;
using DataQI.EntityFrameworkCore.Query.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public class EntitySimpleExpressionTest : IClassFixture<QueryFixture>
    {
        private readonly IEntityCommandBuilder commandBuilder;

        public EntitySimpleExpressionTest(QueryFixture fixture)
        {
            commandBuilder = fixture.GetCommandBuilder();
        }

        [Fact]
        public void TestRejectsNullCriterion()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new EntitySimpleExpression(null));
            var exceptionMessage = exception.GetBaseException().Message;

            Assert.IsType<ArgumentException>(exception.GetBaseException());
            Assert.Equal("SimpleExpression must not be null", exceptionMessage);
        }

        [Fact]
        public void TestBuildEqualExpressionCorrectly()
        {
            var criterion = Restrictions.Equal("FirstName", "Fake Name");
            Assert.Equal("FirstName == @0", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildLikeExpressionCorrectly()
        {
            var criterion = Restrictions.Like("FirstName", "Fake Name");
            Assert.Equal("FirstName.Contains(@0)", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildContainingExpressionCorrectly()
        {
            var criterion = Restrictions.Containing("FirstName", "Fake Name");
            Assert.Equal("FirstName.Contains(@0)", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildStartingWithExpressionCorrectly()
        {
            var criterion = Restrictions.StartingWith("LastName", "Fake Name");
            Assert.Equal("LastName.StartsWith(@0)", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildEndingWithExpressionCorrectly()
        {
            var criterion = Restrictions.EndingWith("LastName", "Fake Name");
            Assert.Equal("LastName.EndsWith(@0)", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildGreaterThanExpressionCorrectly()
        {
            var criterion = Restrictions.GreaterThan("Age", 20);
            Assert.Equal("Age > @0", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildGreaterThanEqualExpressionCorrectly()
        {
            var criterion = Restrictions.GreaterThanEqual("Age", 20);
            Assert.Equal("Age >= @0", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildLessThanExpressionCorrectly()
        {
            var criterion = Restrictions.LessThan("Age", 20);
            Assert.Equal("Age < @0", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildLessThanEqualExpressionCorrectly()
        {
            var criterion = Restrictions.LessThanEqual("Age", 20);
            Assert.Equal("Age <= @0", criterion.GetExpressionBuilder().Build(commandBuilder));
        }

    }
}