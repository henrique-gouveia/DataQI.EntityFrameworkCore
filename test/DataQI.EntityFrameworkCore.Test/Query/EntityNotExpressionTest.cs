using System;

using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query;
using DataQI.EntityFrameworkCore.Query.Extensions;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query
{
    public class EntityNotExpressionTest : IClassFixture<QueryFixture>
    {
        private readonly IEntityCommandBuilder commandBuilder;

        public EntityNotExpressionTest(QueryFixture fixture)
        {
            commandBuilder = fixture.GetCommandBuilder();
        }

        [Fact]
        public void TestRejectsNullCriterion()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new EntityNotExpression(null));
            var exceptionMessage = exception.GetBaseException().Message;

            Assert.IsType<ArgumentException>(exception.GetBaseException());
            Assert.Equal("NotExpression must not be null", exceptionMessage);
        }

        [Fact]
        public void TestBuildNotBetweenExpressionCorrectly()
        {
            var between = Restrictions.Between("DateOfBirth", DateTime.Now.AddYears(-1), DateTime.Now.AddYears(1));
            var notBetween = Restrictions.Not(between);

            Assert.Equal("!(DateOfBirth >= @0 And DateOfBirth <= @1)", notBetween.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildNotStartingWithExpressionCorrectly()
        {
            var startingWith = Restrictions.StartingWith("LastName", "Fake Name");
            var notStartingWith = Restrictions.Not(startingWith);

            Assert.Equal("!LastName.StartsWith(@0)", notStartingWith.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildNotEndingWithExpressionCorrectly()
        {
            var endingWith = Restrictions.EndingWith("LastName", "Fake Name");
            var notEndingWith = Restrictions.Not(endingWith);

            Assert.Equal("!LastName.EndsWith(@0)", notEndingWith.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildNotContainingExpressionCorrectly()
        {
            var containing = Restrictions.Containing("FirstName", "Fake Name");
            var notContaining = Restrictions.Not(containing);

            Assert.Equal("!FirstName.Contains(@0)", notContaining.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildNotLikeExpressionCorrectly()
        {
            var endingWith = Restrictions.Like("LastName", "Fake Name");
            var notEndingWith = Restrictions.Not(endingWith);

            Assert.Equal("!LastName.Contains(@0)", notEndingWith.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildNotEqualExpressionCorrectly()
        {
            var equal = Restrictions.Equal("FirstName", "Fake Name");
            var notEqual = Restrictions.Not(equal);

            Assert.Equal("FirstName != @0", notEqual.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildNotInExpressionCorrectly()
        {
            var In = Restrictions.In("FirstName", new string[] { "Fake Name A", "Fake Name B", "Fake Name C" });
            var notIn = Restrictions.Not(In);

            Assert.Equal("!@0.Contains(FirstName)", notIn.GetExpressionBuilder().Build(commandBuilder));
        }

        [Fact]
        public void TestBuildNotNullExpressionCorrectly()
        {
            var Null = Restrictions.Null("Email");
            var notIn = Restrictions.Not(Null);

            Assert.Equal("Email != null", notIn.GetExpressionBuilder().Build(commandBuilder));
        }
    }
}