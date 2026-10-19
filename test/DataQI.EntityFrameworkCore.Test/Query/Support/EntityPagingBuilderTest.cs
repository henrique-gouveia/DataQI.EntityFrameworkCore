using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Xunit;

using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Fixtures.Paging;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    [Collection(nameof(DatabaseCollection))]
    public sealed class EntityPagingBuilderTest : IClassFixture<DbFixture>
    {
        private readonly TestContext context;

        public EntityPagingBuilderTest(DbFixture fixture)
        {
            context = fixture.ProductContext;
        }

        [Fact]
        public void TestBuildPagedQueryOrdersByAShadowKey()
        {
            using var context = ShadowKeyContext.CreateSeeded(rows: 5);
            var orders = new[] { Order.Asc(nameof(ShadowKeyEntity.Name)), Order.Asc("ShadowId") };
            var entityType = context.Model.FindEntityType(typeof(ShadowKeyEntity));

            var query = EntityPagingBuilder<ShadowKeyEntity>.BuildPagedQuery(
                context.Set<ShadowKeyEntity>(), orders, entityType, skip: 0, take: 10);

            Assert.Equal(Enumerable.Range(0, 5), query.Select(entity => entity.Marker));
        }

        [Fact]
        public void TestBuildPagedQueryOrdersByEveryCompositeKeyProperty()
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

            var query = EntityPagingBuilder<CompositeKeyEntity>.BuildPagedQuery(
                context.Set<CompositeKeyEntity>(), orders, entityType, skip: 0, take: 10);

            Assert.Equal(expectedKeys, query.AsEnumerable().Select(entity => (entity.A, entity.B)));
        }

        [Fact]
        public void TestBuildPagedQueryRejectsUnknownOrderProperty()
        {
            Assert.Throws<InvalidOperationException>(() => EntityPagingBuilder<Product>.BuildPagedQuery(
                context.Set<Product>(), new[] { Order.Asc("Nope") }, context.Model.FindEntityType(typeof(Product)), 0, 10));
        }

        [Fact]
        public void TestBuildPagedQueryTurnsSkipAndTakeIntoSqlParameters()
        {
            var query = EntityPagingBuilder<Product>.BuildPagedQuery(
                context.Set<Product>().AsNoTracking(),
                new[] { Order.Asc("Id") }, context.Model.FindEntityType(typeof(Product)), skip: 20, take: 10);

            var sql = query.ToQueryString();

            Assert.Contains("LIMIT @", sql);
            Assert.Contains("OFFSET @", sql);
        }

        [Fact]
        public void TestBuildPagedQueryUsesTheMappedHiddenProperty()
        {
            using var hidden = new HiddenPropertyContext();

            var query = EntityPagingBuilder<HiddenPropertyEntity>.BuildPagedQuery(
                hidden.Set<HiddenPropertyEntity>(), new[] { Order.Asc(nameof(HiddenPropertyEntity.Name)) }, hidden.Model.FindEntityType(typeof(HiddenPropertyEntity)), 0, 10);

            Assert.Equal(new[] { 1, 2 }, query.Select(e => e.Name).ToArray());
        }

        [Fact]
        public void TestBuildPagedQueryRejectsAnUnmappedClrProperty()
        {
            using var hidden = new HiddenPropertyContext();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                EntityPagingBuilder<HiddenPropertyEntity>.BuildPagedQuery(hidden.Set<HiddenPropertyEntity>(),
                    new[] { Order.Asc(nameof(HiddenPropertyEntity.Unmapped)) }, hidden.Model.FindEntityType(typeof(HiddenPropertyEntity)), 0, 10));

            Assert.Equal("Cannot order HiddenPropertyEntity by unknown property 'Unmapped'.", exception.Message);
        }

        private class HiddenPropertyBase
        {
            public string Name { get; set; }
        }

        private sealed class HiddenPropertyEntity : HiddenPropertyBase
        {
            public int Id { get; set; }
            public new int Name { get; set; }

            [System.ComponentModel.DataAnnotations.Schema.NotMapped]
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
