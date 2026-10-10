using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using DataQI.Commons.Query.Ast;
using DataQI.Commons.Query.Support;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    public class EntityPredicateVisitorStringComparisonTest : IDisposable
    {
        private readonly StringContext context;

        public EntityPredicateVisitorStringComparisonTest()
        {
            var options = new DbContextOptionsBuilder<StringContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
            context = new StringContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            context.AddRange(
                new StringEntity { Id = 1, Name = "Adams" },
                new StringEntity { Id = 2, Name = "Miller" },
                new StringEntity { Id = 3, Name = "Zulu" },
                new StringEntity { Id = 4, Name = null });
            context.SaveChanges();
        }

        [Theory]
        [InlineData(ComparisonKind.GreaterThan, "3")]
        [InlineData(ComparisonKind.GreaterThanEqual, "2,3")]
        [InlineData(ComparisonKind.LessThan, "1")]
        [InlineData(ComparisonKind.LessThanEqual, "1,2")]
        [InlineData(ComparisonKind.Equal, "2")]
        public void TestStringComparisonExecutesOnSqlite(ComparisonKind kind, string expectedIds)
        {
            var criteria = new Criteria().Add(new Comparison("Name", kind, "Miller"));
            var predicate = EntityPredicateVisitor<StringEntity>.BuildPredicate(criteria);
            var query = context.Set<StringEntity>().Where(predicate);
            var sql = query.ToQueryString();
            var ids = query.OrderBy(entity => entity.Id).Select(entity => entity.Id).ToArray();

            Assert.Contains("WHERE", sql);
            Assert.Equal(expectedIds, string.Join(",", ids));
        }

        [Theory]
        [InlineData("Adams", "Miller", "1,2")]
        [InlineData("Miller", "Miller", "2")]
        public void TestStringBetweenExecutesInclusivelyOnSqlite(string starts, string ends, string expectedIds)
        {
            var criteria = new Criteria().Add(Restrictions.Between("Name", starts, ends));
            var predicate = EntityPredicateVisitor<StringEntity>.BuildPredicate(criteria);
            var query = context.Set<StringEntity>().Where(predicate);
            var sql = query.ToQueryString();
            var ids = query.OrderBy(entity => entity.Id).Select(entity => entity.Id).ToArray();

            Assert.Contains("WHERE", sql);
            Assert.Equal(expectedIds, string.Join(",", ids));
        }

        [Theory]
        [InlineData(ComparisonKind.GreaterThan, false)]
        [InlineData(ComparisonKind.GreaterThanEqual, false)]
        [InlineData(ComparisonKind.LessThan, true)]
        [InlineData(ComparisonKind.LessThanEqual, true)]
        public void TestNullStringPropertyUsesCompareSemanticsInMemory(ComparisonKind kind, bool expected)
        {
            var criteria = new Criteria().Add(new Comparison("Name", kind, "Miller"));
            var predicate = EntityPredicateVisitor<StringEntity>.BuildPredicate(criteria).Compile();

            Assert.Equal(expected, predicate(new StringEntity { Name = null }));
        }

        [Theory]
        [InlineData(ComparisonKind.GreaterThan)]
        [InlineData(ComparisonKind.GreaterThanEqual)]
        [InlineData(ComparisonKind.LessThan)]
        [InlineData(ComparisonKind.LessThanEqual)]
        public void TestStringComparisonRejectsNullLimit(ComparisonKind kind)
        {
            var criteria = new Criteria().Add(new Comparison("Name", kind, null));

            var exception = Assert.Throws<ArgumentException>(() =>
                EntityPredicateVisitor<StringEntity>.BuildPredicate(criteria));

            Assert.Equal("value", exception.ParamName);
        }

        [Theory]
        [InlineData(null, "Miller")]
        [InlineData("Adams", null)]
        [InlineData(null, null)]
        public void TestStringBetweenRejectsNullLimits(string starts, string ends)
        {
            var criteria = new Criteria().Add(Restrictions.Between("Name", starts, ends));

            Assert.Throws<ArgumentException>(() =>
                EntityPredicateVisitor<StringEntity>.BuildPredicate(criteria));
        }

        [Fact]
        public void TestStringEqualityWithNullStillExecutesOnSqlite()
        {
            var criteria = new Criteria().Add(Restrictions.Equal("Name", null));
            var predicate = EntityPredicateVisitor<StringEntity>.BuildPredicate(criteria);
            var result = context.Set<StringEntity>().Where(predicate).Single();

            Assert.Equal(4, result.Id);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFindByNameGreaterThanRejectsNullThroughProxy(bool useAsyncMethod)
        {
            var repository = new EntityRepositoryFactory().GetRepository<IStringRepository>(context);
            var exception = useAsyncMethod
                ? await Assert.ThrowsAnyAsync<Exception>(() => repository.FindByNameGreaterThanAsync(null))
                : Assert.ThrowsAny<Exception>(() => repository.FindByNameGreaterThan(null));
            var argumentException = Assert.IsType<ArgumentException>(exception.GetBaseException());

            Assert.Equal("value", argumentException.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFindByNameGreaterThanThroughProxy(bool useAsyncMethod)
        {
            var repository = new EntityRepositoryFactory().GetRepository<IStringRepository>(context);
            var result = useAsyncMethod
                ? await repository.FindByNameGreaterThanAsync("M")
                : repository.FindByNameGreaterThan("M");
            var ids = result.OrderBy(entity => entity.Id).Select(entity => entity.Id).ToArray();

            Assert.Equal(new[] { 2, 3 }, ids);
        }

        public void Dispose()
        {
            context.Dispose();
        }

        public class StringEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public interface IStringRepository : IEntityRepository<StringEntity, int>
        {
            IEnumerable<StringEntity> FindByNameGreaterThan(string name);
            Task<IEnumerable<StringEntity>> FindByNameGreaterThanAsync(string name, CancellationToken cancellationToken = default);
        }

        private sealed class StringContext : DbContext
        {
            public StringContext(DbContextOptions<StringContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<StringEntity>();
            }
        }
    }
}
