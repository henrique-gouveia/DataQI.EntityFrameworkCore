using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    public class EntityPredicateVisitorParameterizationTest
    {
        [Theory]
        [InlineData("Comparison")]
        [InlineData("Between")]
        [InlineData("TextMatch")]
        [InlineData("GreaterThan")]
        [InlineData("GreaterThanEqual")]
        [InlineData("LessThan")]
        [InlineData("LessThanEqual")]
        [InlineData("StringComparison")]
        [InlineData("StringGreaterThan")]
        [InlineData("StringGreaterThanEqual")]
        [InlineData("StringLessThan")]
        [InlineData("StringLessThanEqual")]
        [InlineData("StringBetween")]
        [InlineData("StartingWith")]
        [InlineData("EndingWith")]
        [InlineData("Like")]
        public void TestDifferentValuesKeepSameSqlCommand(string criterionKind)
        {
            using (var context = NewContext())
            {
                var firstSql = QuerySql(context, criterionKind, 10);
                var secondSql = QuerySql(context, criterionKind, 20);
                var firstCommand = firstSql.Substring(firstSql.IndexOf("SELECT", StringComparison.Ordinal));
                var secondCommand = secondSql.Substring(secondSql.IndexOf("SELECT", StringComparison.Ordinal));

                Assert.Contains("@", firstCommand);
                Assert.Equal(firstCommand, secondCommand);
            }
        }

        [Theory]
        [InlineData("Comparison")]
        [InlineData("Between")]
        [InlineData("TextMatch")]
        [InlineData("GreaterThan")]
        [InlineData("GreaterThanEqual")]
        [InlineData("LessThan")]
        [InlineData("LessThanEqual")]
        [InlineData("StringComparison")]
        [InlineData("StringGreaterThan")]
        [InlineData("StringGreaterThanEqual")]
        [InlineData("StringLessThan")]
        [InlineData("StringLessThanEqual")]
        [InlineData("StringBetween")]
        [InlineData("StartingWith")]
        [InlineData("EndingWith")]
        [InlineData("Like")]
        public void TestDifferentValuesReuseCompiledQuery(string criterionKind)
        {
            var compilationCount = 0;
            using (var context = NewContext(message => compilationCount++))
            {
                QuerySql(context, criterionKind, 10);
                QuerySql(context, criterionKind, 20);

                Assert.Equal(1, compilationCount);
            }
        }

        [Theory]
        [InlineData("Comparison")]
        [InlineData("Between")]
        [InlineData("TextMatch")]
        [InlineData("GreaterThan")]
        [InlineData("GreaterThanEqual")]
        [InlineData("LessThan")]
        [InlineData("LessThanEqual")]
        [InlineData("StringComparison")]
        [InlineData("StringGreaterThan")]
        [InlineData("StringGreaterThanEqual")]
        [InlineData("StringLessThan")]
        [InlineData("StringLessThanEqual")]
        [InlineData("StringBetween")]
        [InlineData("StartingWith")]
        [InlineData("EndingWith")]
        [InlineData("Like")]
        public void TestSameValueReusesCompiledQuery(string criterionKind)
        {
            var compilationCount = 0;
            using (var context = NewContext(message => compilationCount++))
            {
                QuerySql(context, criterionKind, 10);
                QuerySql(context, criterionKind, 10);

                Assert.Equal(1, compilationCount);
            }
        }

        [Theory]
        [InlineData("Id", false)]
        [InlineData("Id", true)]
        [InlineData("Name", false)]
        [InlineData("Name", true)]
        public void TestInWithDifferentListsReusesCompiledQuery(string propertyName, bool differentLength)
        {
            var compilationCount = 0;
            using (var context = NewContext(message => compilationCount++))
            {
                var firstValues = propertyName == "Id"
                    ? new object[] { 10, 15 }
                    : new object[] { "Adams", "Barnes" };
                var secondValues = propertyName == "Id"
                    ? (differentLength ? new object[] { 20, 25, 30 } : new object[] { 20, 25 })
                    : (differentLength ? new object[] { "Miller", "Smith", "Zulu" } : new object[] { "Miller", "Smith" });

                InQuery(context, propertyName, firstValues).ToQueryString();
                Assert.Equal(1, compilationCount);

                InQuery(context, propertyName, secondValues).ToQueryString();

                Assert.Equal(1, compilationCount);
            }
        }

        [Theory]
        [InlineData("Id")]
        [InlineData("Name")]
        public void TestInWithSameQueryReusesCompiledQuery(string propertyName)
        {
            var compilationCount = 0;
            using (var context = NewContext(message => compilationCount++))
            {
                var values = propertyName == "Id"
                    ? new object[] { 10, 15 }
                    : new object[] { "Adams", "Barnes" };
                var query = InQuery(context, propertyName, values);

                query.ToQueryString();
                Assert.Equal(1, compilationCount);

                query.ToQueryString();

                Assert.Equal(1, compilationCount);
            }
        }

        [Theory]
        [InlineData("Id", typeof(int[]))]
        [InlineData("Name", typeof(string[]))]
        public void TestInDoesNotEmbedCollectionAsConstant(string propertyName, Type expectedType)
        {
            var values = propertyName == "Id"
                ? new object[] { 10, 15 }
                : new object[] { "Adams", "Barnes" };
            var criteria = new Criteria().Add(Restrictions.In(propertyName, values));
            var predicate = EntityPredicateVisitor<Product>.BuildPredicate(criteria);
            var contains = Assert.IsAssignableFrom<MethodCallExpression>(predicate.Body);
            var collection = Assert.IsAssignableFrom<MemberExpression>(contains.Arguments[0]);

            Assert.Equal(expectedType, collection.Type);
        }

        [Fact]
        public void TestCapturedComparisonRetainsNullableNumericType()
        {
            var criteria = new Criteria().Add(Restrictions.Equal("Amount", 10));
            var predicate = EntityPredicateVisitor<NullableEntity>.BuildPredicate(criteria);
            var comparison = Assert.IsAssignableFrom<BinaryExpression>(predicate.Body);
            var compiled = predicate.Compile();

            Assert.Equal(typeof(decimal?), comparison.Right.Type);
            Assert.True(compiled(new NullableEntity { Amount = 10m }));
            Assert.False(compiled(new NullableEntity { Amount = 20m }));
            Assert.False(compiled(new NullableEntity { Amount = null }));
        }

        [Theory]
        [InlineData(10, 1)]
        [InlineData(null, 3)]
        public void TestNullableComparisonExecutesOnSqliteWithConvertedValue(object value, int expectedId)
        {
            using (var context = NewContext())
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();
                context.AddRange(
                    new NullableEntity { Id = 1, Amount = 10m },
                    new NullableEntity { Id = 2, Amount = 20m },
                    new NullableEntity { Id = 3, Amount = null });
                context.SaveChanges();
                var criteria = new Criteria().Add(Restrictions.Equal("Amount", value));
                var predicate = EntityPredicateVisitor<NullableEntity>.BuildPredicate(criteria);
                var result = context.Set<NullableEntity>().Where(predicate).Single();

                Assert.Equal(expectedId, result.Id);
            }
        }

        [Fact]
        public void TestComparisonRejectsNullForNonNullableValueType()
        {
            var criteria = new Criteria().Add(Restrictions.Equal("Id", null));

            Assert.Throws<ArgumentException>(() => EntityPredicateVisitor<Product>.BuildPredicate(criteria));
        }

        [Theory]
        [InlineData("Id", 10, 20)]
        [InlineData("Name", "Adams", "Barnes")]
        public void TestCapturedValuesRemainIndependentBetweenPredicates(string propertyName, object firstValue, object secondValue)
        {
            var first = EntityPredicateVisitor<Product>.BuildPredicate(
                new Criteria().Add(Restrictions.Equal(propertyName, firstValue)));
            var second = EntityPredicateVisitor<Product>.BuildPredicate(
                new Criteria().Add(Restrictions.Equal(propertyName, secondValue)));
            var firstPredicate = first.Compile();
            var secondPredicate = second.Compile();
            var firstEntity = new Product { Id = 10, Name = "Adams" };
            var secondEntity = new Product { Id = 20, Name = "Barnes" };

            Assert.True(firstPredicate(firstEntity));
            Assert.False(firstPredicate(secondEntity));
            Assert.False(secondPredicate(firstEntity));
            Assert.True(secondPredicate(secondEntity));
        }

        [Fact]
        public void TestCapturedNullableValuesRemainIndependentBetweenPredicates()
        {
            var first = EntityPredicateVisitor<NullableEntity>.BuildPredicate(
                new Criteria().Add(Restrictions.Equal("Amount", 10)));
            var second = EntityPredicateVisitor<NullableEntity>.BuildPredicate(
                new Criteria().Add(Restrictions.Equal("Amount", null)));
            var firstPredicate = first.Compile();
            var secondPredicate = second.Compile();

            Assert.True(firstPredicate(new NullableEntity { Amount = 10m }));
            Assert.False(firstPredicate(new NullableEntity { Amount = null }));
            Assert.False(secondPredicate(new NullableEntity { Amount = 10m }));
            Assert.True(secondPredicate(new NullableEntity { Amount = null }));
        }

        [Theory]
        [InlineData("Id")]
        [InlineData("Name")]
        public void TestCapturedInValuesRemainIndependentBetweenPredicates(string propertyName)
        {
            var firstValues = propertyName == "Id" ? new object[] { 10 } : new object[] { "Adams" };
            var secondValues = propertyName == "Id" ? new object[] { 20 } : new object[] { "Barnes" };
            var first = EntityPredicateVisitor<Product>.BuildPredicate(
                new Criteria().Add(Restrictions.In(propertyName, firstValues)));
            var second = EntityPredicateVisitor<Product>.BuildPredicate(
                new Criteria().Add(Restrictions.In(propertyName, secondValues)));
            var firstPredicate = first.Compile();
            var secondPredicate = second.Compile();
            var firstEntity = new Product { Id = 10, Name = "Adams" };
            var secondEntity = new Product { Id = 20, Name = "Barnes" };

            Assert.True(firstPredicate(firstEntity));
            Assert.False(firstPredicate(secondEntity));
            Assert.False(secondPredicate(firstEntity));
            Assert.True(secondPredicate(secondEntity));
        }

        private static IQueryable<Product> InQuery(DbContext context, string propertyName, object[] values)
        {
            var criteria = new Criteria().Add(Restrictions.In(propertyName, values));
            var predicate = EntityPredicateVisitor<Product>.BuildPredicate(criteria);

            return context.Set<Product>().Where(predicate);
        }

        private static string QuerySql(DbContext context, string criterionKind, int value)
        {
            var criteria = new Criteria().Add(Criterion(criterionKind, value));
            var predicate = EntityPredicateVisitor<Product>.BuildPredicate(criteria);

            return context.Set<Product>().Where(predicate).ToQueryString();
        }

        private static ICriterion Criterion(string criterionKind, int value)
        {
            var text = value.ToString(CultureInfo.InvariantCulture);
            switch (criterionKind)
            {
                case "Comparison": return Restrictions.Equal("Id", value);
                case "Between": return Restrictions.Between("Id", value, value + 5);
                case "TextMatch": return Restrictions.Containing("Name", text);
                case "GreaterThan": return Restrictions.GreaterThan("Id", value);
                case "GreaterThanEqual": return Restrictions.GreaterThanEqual("Id", value);
                case "LessThan": return Restrictions.LessThan("Id", value);
                case "LessThanEqual": return Restrictions.LessThanEqual("Id", value);
                case "StringComparison": return Restrictions.Equal("Name", text);
                case "StringGreaterThan": return Restrictions.GreaterThan("Name", text);
                case "StringGreaterThanEqual": return Restrictions.GreaterThanEqual("Name", text);
                case "StringLessThan": return Restrictions.LessThan("Name", text);
                case "StringLessThanEqual": return Restrictions.LessThanEqual("Name", text);
                case "StringBetween": return Restrictions.Between("Name", text, (value + 5).ToString(CultureInfo.InvariantCulture));
                case "StartingWith": return Restrictions.StartingWith("Name", text);
                case "EndingWith": return Restrictions.EndingWith("Name", text);
                case "Like": return Restrictions.Like("Name", text);
                default: throw new ArgumentException($"Unknown criterion kind '{criterionKind}'.", nameof(criterionKind));
            }
        }

        private static PredicateContext NewContext(Action<string> log = null)
        {
            var options = new DbContextOptionsBuilder<PredicateContext>()
                .UseSqlite("Data Source=:memory:")
                .EnableServiceProviderCaching(false);

            if (log != null)
                options.LogTo(log, new[] { CoreEventId.QueryCompilationStarting });

            return new PredicateContext(options.Options);
        }

        private sealed class NullableEntity
        {
            public int Id { get; set; }
            public decimal? Amount { get; set; }
        }

        private sealed class PredicateContext : DbContext
        {
            public PredicateContext(DbContextOptions<PredicateContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Product>();
                modelBuilder.Entity<NullableEntity>();
            }
        }
    }
}
