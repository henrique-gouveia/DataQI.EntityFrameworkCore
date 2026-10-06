using System;
using System.Linq;

using DataQI.Commons.Query.Support;
using DataQI.Commons.Query.Ast;
using DataQI.Commons.Query;
using DataQI.EntityFrameworkCore.Query.Support;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    public class EntityPredicateVisitorTest
    {
        private readonly FakeEntity[] entities = new[]
        {
            new FakeEntity { Name = "Adams", Stock = 10m, BirthDate = new DateTime(1990, 1, 1), Email = "adams@example.com" },
            new FakeEntity { Name = "Barnes", Stock = 30m, BirthDate = new DateTime(1985, 6, 1), Email = null },
        };

        [Fact]
        public void TestComparisonMatchesCorrectly()
        {
            var criteria = new Criteria().Add(Restrictions.Equal("Name", "Adams"));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
            Assert.Equal("Adams", entities.Single(predicate).Name);
        }

        [Fact]
        public void TestComparisonCoercesMismatchedNumericType()
        {
            var criteria = new Criteria().Add(Restrictions.Equal("Stock", 10));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
        }

        [Fact]
        public void TestBetweenIsInclusiveBothEnds()
        {
            var criteria = new Criteria().Add(Restrictions.Between("Stock", 10m, 30m));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Equal(2, entities.Count(predicate));
        }

        [Fact]
        public void TestBetweenCoercesMismatchedNumericType()
        {
            var predicate = Compile(new Criteria().Add(Restrictions.Between("Stock", 10, 30)));

            Assert.Equal(2, entities.Count(predicate));
        }

        [Fact]
        public void TestInCoercesMismatchedNumericType()
        {
            var predicate = Compile(new Criteria().Add(Restrictions.In("Stock", new object[] { 10, 30 })));

            Assert.Equal(2, entities.Count(predicate));
        }

        [Fact]
        public void TestInMatchesCorrectly()
        {
            var criteria = new Criteria().Add(Restrictions.In("Name", new object[] { "Adams" }));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
        }

        [Fact]
        public void TestIsNullMatchesCorrectlyOnNullableProperty()
        {
            var criteria = new Criteria().Add(Restrictions.Null("Email"));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
            Assert.Equal("Barnes", entities.Single(predicate).Name);
        }

        [Fact]
        public void TestIsNullThrowsClearlyOnNonNullableValueTypeProperty()
        {
            var criteria = new Criteria().Add(Restrictions.Null("BirthDate"));

            var exception = Assert.Throws<InvalidOperationException>(() =>
                EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria));

            Assert.Contains("BirthDate", exception.Message);
            Assert.Contains("non-nullable", exception.Message);
        }

        [Fact]
        public void TestTextMatchContainingMatchesCorrectly()
        {
            var criteria = new Criteria().Add(Restrictions.Containing("Name", "dam"));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
        }

        [Fact]
        public void TestNotNegatesInnerCorrectly()
        {
            var criteria = new Criteria().Add(Restrictions.Not(Restrictions.Equal("Name", "Adams")));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
            Assert.Equal("Barnes", entities.Single(predicate).Name);
        }

        [Fact]
        public void TestJunctionAndCorrectly()
        {
            var criteria = new Criteria().Add(Restrictions
                .Conjunction()
                .Add(Restrictions.Equal("Name", "Adams"))
                .Add(Restrictions.Equal("Stock", 10m)));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
        }

        [Fact]
        public void TestEmptyCriteriaMatchesEveryEntity()
        {
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(new Criteria()).Compile();

            Assert.Equal(entities.Length, entities.Count(predicate));
        }

        [Fact]
        public void TestMultipleTopLevelCriterionsJoinedWithAnd()
        {
            var criteria = new Criteria()
                .Add(Restrictions.Equal("Name", "Adams"))
                .Add(Restrictions.Equal("Stock", 10m));
            var predicate = EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

            Assert.Single(entities.Where(predicate));
        }


        [Theory]
        [InlineData(ComparisonKind.GreaterThan, 10, 1)]
        [InlineData(ComparisonKind.GreaterThanEqual, 10, 2)]
        [InlineData(ComparisonKind.LessThan, 30, 1)]
        [InlineData(ComparisonKind.LessThanEqual, 30, 2)]
        public void TestEveryComparisonKindMatchesCorrectly(ComparisonKind kind, int value, int expectedCount)
        {
            var predicate = Compile(new Criteria().Add(new Comparison("Stock", kind, value)));

            Assert.Equal(expectedCount, entities.Count(predicate));
        }

        [Theory]
        [InlineData(TextMatchKind.StartingWith, "Ad", "Adams")]
        [InlineData(TextMatchKind.EndingWith, "es", "Barnes")]
        [InlineData(TextMatchKind.Containing, "dam", "Adams")]
        [InlineData(TextMatchKind.Like, "arn", "Barnes")]
        public void TestEveryTextMatchKindMatchesCorrectly(TextMatchKind kind, string value, string expectedName)
        {
            var predicate = Compile(new Criteria().Add(new TextMatch("Name", kind, value)));

            Assert.Equal(expectedName, entities.Single(predicate).Name);
        }

        [Fact]
        public void TestNotOverEveryCriterionCorrectly()
        {
            AssertNotMatches(Restrictions.Between("Stock", 10m, 10m), "Barnes");
            AssertNotMatches(Restrictions.StartingWith("Name", "Ad"), "Barnes");
            AssertNotMatches(Restrictions.EndingWith("Name", "es"), "Adams");
            AssertNotMatches(Restrictions.Containing("Name", "dam"), "Barnes");
            AssertNotMatches(Restrictions.Like("Name", "arn"), "Adams");
            AssertNotMatches(Restrictions.Equal("Name", "Adams"), "Barnes");
            AssertNotMatches(Restrictions.In("Name", new object[] { "Adams" }), "Barnes");
            AssertNotMatches(Restrictions.Null("Email"), "Adams");
        }

        [Fact]
        public void TestJunctionOrCorrectly()
        {
            var criteria = new Criteria().Add(new Junction(LogicalKind.Or)
                .Add(Restrictions.Equal("Name", "Adams"))
                .Add(Restrictions.Equal("Stock", 30m)));

            Assert.Equal(2, entities.Count(Compile(criteria)));
        }

        [Fact]
        public void TestNestedJunctionsCorrectly()
        {
            var criteria = new Criteria().Add(new Junction(LogicalKind.Or)
                .Add(new Junction(LogicalKind.And)
                    .Add(Restrictions.Equal("Name", "Adams"))
                    .Add(Restrictions.Equal("Stock", 99m)))
                .Add(new Junction(LogicalKind.And)
                    .Add(Restrictions.Equal("Name", "Barnes"))));

            Assert.Equal("Barnes", entities.Single(Compile(criteria)).Name);
        }

        private Func<FakeEntity, bool> Compile(ICriteria criteria)
            => EntityPredicateVisitor<FakeEntity>.BuildPredicate(criteria).Compile();

        private void AssertNotMatches(ICriterion inner, string expectedName)
            => Assert.Equal(expectedName, entities.Single(Compile(new Criteria().Add(Restrictions.Not(inner)))).Name);

        public class FakeEntity
        {
            public string Name { get; set; }
            public decimal Stock { get; set; }
            public DateTime BirthDate { get; set; }
            public string Email { get; set; }
        }
    }
}
