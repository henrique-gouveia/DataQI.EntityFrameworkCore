using System;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Xunit;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Fixtures.Paging;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    [Collection(nameof(DatabaseCollection))]
    public sealed class EntityExpressionBuilderTest : IClassFixture<DbFixture>
    {
        private readonly TestContext context;

        public EntityExpressionBuilderTest(DbFixture fixture)
        {
            context = fixture.ProductContext;
        }

        [Fact]
        public void TestEmptyFilterDoesNotAddWhereToFindOrBuildWhere()
        {
            var source = context.Set<Product>().AsNoTracking();
            var filter = Empty<Product>();
            var entityType = context.Model.FindEntityType(typeof(Product));

            var find = EntityExpressionBuilder<Product>.BuildFind(
                source, filter, Array.Empty<IOrderCriterion>(), entityType);
            var where = EntityExpressionBuilder<Product>.BuildWhere(source, filter);

            Assert.Same(source, find);
            Assert.Same(source, where);
        }

        [Fact]
        public void TestFindOrdersAscendingBySingleProperty()
        {
            using var context = CompositeKeyContext.CreateSeeded(rows: 6);
            var query = Find(context, new[] { Order.Asc(nameof(CompositeKeyEntity.A)) });

            Assert.Equal(new[] { 0, 0, 1, 1, 2, 2 }, query.Select(e => e.A));
        }

        [Fact]
        public void TestFindOrdersDescendingBySingleProperty()
        {
            using var context = CompositeKeyContext.CreateSeeded(rows: 6);
            var query = Find(context, new[] { Order.Desc(nameof(CompositeKeyEntity.A)) });

            Assert.Equal(new[] { 2, 2, 1, 1, 0, 0 }, query.Select(e => e.A));
        }

        [Fact]
        public void TestFindOrdersByMultiplePropertiesInSequence()
        {
            using var context = CompositeKeyContext.CreateSeeded(rows: 6);
            var query = Find(context, new[] { Order.Asc(nameof(CompositeKeyEntity.A)),
                Order.Desc(nameof(CompositeKeyEntity.B)) });

            Assert.Equal(new[] { (0, 1), (0, 0), (1, 1), (1, 0), (2, 1), (2, 0) },
                query.Select(e => new { e.A, e.B }).AsEnumerable().Select(e => (e.A, e.B)));
        }

        [Fact]
        public void TestFindReturnsTheSameQueryWhenFilterAndOrdersAreEmpty()
        {
            using var context = CompositeKeyContext.CreateSeeded(rows: 6);
            var source = context.Set<CompositeKeyEntity>().AsNoTracking();

            var query = EntityExpressionBuilder<CompositeKeyEntity>.BuildFind(source,
                Empty<CompositeKeyEntity>(), Array.Empty<IOrderCriterion>(),
                context.Model.FindEntityType(typeof(CompositeKeyEntity)));

            Assert.Same(source, query);
        }

        [Fact]
        public void TestFindRejectsUnknownOrderProperty()
        {
            using var context = CompositeKeyContext.CreateSeeded(rows: 6);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                Find(context, new[] { Order.Asc("DoesNotExist") }));

            Assert.Equal("Cannot order CompositeKeyEntity by unknown property 'DoesNotExist'.",
                exception.Message);
        }

        [Fact]
        public void TestFindRejectsAnUnmappedClrProperty()
        {
            using var hidden = new HiddenPropertyContext();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                EntityExpressionBuilder<HiddenPropertyEntity>.BuildFind(hidden.Set<HiddenPropertyEntity>(),
                    Empty<HiddenPropertyEntity>(), new[] { Order.Asc(nameof(HiddenPropertyEntity.Unmapped)) },
                    hidden.Model.FindEntityType(typeof(HiddenPropertyEntity))));

            Assert.Equal("Cannot order HiddenPropertyEntity by unknown property 'Unmapped'.", exception.Message);
        }

        [Fact]
        public void TestFindCanOrderByAShadowKey()
        {
            using var context = ShadowKeyContext.CreateSeeded(rows: 5);
            var query = EntityExpressionBuilder<ShadowKeyEntity>.BuildFind(context.Set<ShadowKeyEntity>(),
                Empty<ShadowKeyEntity>(), new[] { Order.Asc("ShadowId") },
                context.Model.FindEntityType(typeof(ShadowKeyEntity)));

            Assert.Equal(Enumerable.Range(0, 5), query.Select(e => e.Marker));
        }

        private static IQueryable<CompositeKeyEntity> Find(CompositeKeyContext context,
            IOrderCriterion[] orders)
            => EntityExpressionBuilder<CompositeKeyEntity>.BuildFind(context.Set<CompositeKeyEntity>(),
                Empty<CompositeKeyEntity>(), orders,
                context.Model.FindEntityType(typeof(CompositeKeyEntity)));

        [Fact]
        public void TestBuildPageOrdersByAShadowKey()
        {
            using var context = ShadowKeyContext.CreateSeeded(rows: 5);
            var orders = new[] { Order.Asc(nameof(ShadowKeyEntity.Name)), Order.Asc("ShadowId") };
            var entityType = context.Model.FindEntityType(typeof(ShadowKeyEntity));

            var query = EntityExpressionBuilder<ShadowKeyEntity>.BuildPage(
                context.Set<ShadowKeyEntity>(), Empty<ShadowKeyEntity>(), orders, entityType, skip: 0, take: 10);

            Assert.Equal(Enumerable.Range(0, 5), query.Select(entity => entity.Marker));
        }

        [Fact]
        public void TestBuildPageOrdersByEveryCompositeKeyProperty()
        {
            using var context = CompositeKeyContext.CreateSeeded(rows: 6);
            var orders = new[]
            {
                Order.Asc(nameof(CompositeKeyEntity.Name)),
                Order.Asc(nameof(CompositeKeyEntity.A)),
                Order.Asc(nameof(CompositeKeyEntity.B))
            };
            var entityType = context.Model.FindEntityType(typeof(CompositeKeyEntity));
            var expectedKeys = Enumerable.Range(0, 6).Select(index => (index / 2, index % 2));

            var query = EntityExpressionBuilder<CompositeKeyEntity>.BuildPage(
                context.Set<CompositeKeyEntity>(), Empty<CompositeKeyEntity>(), orders, entityType, skip: 0, take: 10);

            Assert.Equal(expectedKeys, query.AsEnumerable().Select(entity => (entity.A, entity.B)));
        }

        [Fact]
        public void TestBuildPageRejectsUnknownOrderProperty()
        {
            Assert.Throws<InvalidOperationException>(() => EntityExpressionBuilder<Product>.BuildPage(
                context.Set<Product>(), Empty<Product>(), new[] { Order.Asc("Nope") }, context.Model.FindEntityType(typeof(Product)), 0, 10));
        }

        [Fact]
        public void TestBuildPageTurnsSkipAndTakeIntoSqlParameters()
        {
            var query = EntityExpressionBuilder<Product>.BuildPage(
                context.Set<Product>().AsNoTracking(), Empty<Product>(),
                new[] { Order.Asc("Id") }, context.Model.FindEntityType(typeof(Product)), skip: 20, take: 10);

            var sql = query.ToQueryString();

            Assert.Contains("LIMIT @", sql);
            Assert.Contains("OFFSET @", sql);
        }

        [Fact]
        public void TestBuildPageUsesTheMappedHiddenProperty()
        {
            using var hidden = new HiddenPropertyContext();

            var query = EntityExpressionBuilder<HiddenPropertyEntity>.BuildPage(
                hidden.Set<HiddenPropertyEntity>(), Empty<HiddenPropertyEntity>(), new[] { Order.Asc(nameof(HiddenPropertyEntity.Name)) }, hidden.Model.FindEntityType(typeof(HiddenPropertyEntity)), 0, 10);

            Assert.Equal(new[] { 1, 2 }, query.Select(e => e.Name).ToArray());
        }

        [Fact]
        public void TestBuildPageRejectsAnUnmappedClrProperty()
        {
            using var hidden = new HiddenPropertyContext();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                EntityExpressionBuilder<HiddenPropertyEntity>.BuildPage(hidden.Set<HiddenPropertyEntity>(), Empty<HiddenPropertyEntity>(),
                    new[] { Order.Asc(nameof(HiddenPropertyEntity.Unmapped)) }, hidden.Model.FindEntityType(typeof(HiddenPropertyEntity)), 0, 10));

            Assert.Equal("Cannot order HiddenPropertyEntity by unknown property 'Unmapped'.", exception.Message);
        }

        private static EntityPredicate<TEntity> Empty<TEntity>()
            => EntityPredicateVisitor<TEntity>.BuildPredicate(new Criteria());

        private class HiddenPropertyBase
        {
            public string Name { get; set; }
        }

        private sealed class HiddenPropertyEntity : HiddenPropertyBase
        {
            public int Id { get; set; }
            public new int Name { get; set; }

            [NotMapped]
            public string Unmapped { get; set; }
        }

        private sealed class HiddenPropertyContext : DbContext
        {
            public HiddenPropertyContext() : base(new DbContextOptionsBuilder().UseSqlite("Data Source=:memory:").Options)
            {
                Database.OpenConnection();
                Database.EnsureCreated();
                AddRange(new HiddenPropertyEntity { Id = 1, Name = 2 }, new HiddenPropertyEntity { Id = 2, Name = 1 });
                SaveChanges();
                ChangeTracker.Clear();
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<HiddenPropertyEntity>().HasKey(e => e.Id);
                modelBuilder.Entity<HiddenPropertyEntity>().Property(e => e.Name);
            }
        }
    }
}
