using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Repository.Paging;
using DataQI.Commons.Repository.Paging.Support;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Repository.Products;
using DataQI.EntityFrameworkCore.Test.Fixtures;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    [Collection(nameof(DatabaseCollection))]
    public class EntityPagingBuilderTest : IClassFixture<DbFixture>
    {
        private readonly TestContext context;

        public EntityPagingBuilderTest(DbFixture fixture)
        {
            context = fixture.ProductContext;
        }

        [Fact]
        public void TestKeyPropertiesComeFromTheModel()
        {
            Assert.Equal(new[] { nameof(Product.Id) },
                EntityPagingBuilder<Product>.KeyProperties(context).Select(k => k.Name));
        }

        [Fact]
        public void TestBuildPagedQueryOrdersByAShadowKey()
        {
            using (var context = ShadowKeyContext.CreateSeeded(rows: 5))
            {
                var keys = EntityPagingBuilder<ShadowKeyEntity>.KeyProperties(context);
                var orders = PagingRules.MergeOrders<ShadowKeyEntity>(
                    new[] { Order.Asc("Name") }, PageRequest.Of(0, 10), keys.Select(k => k.Name));

                var query = EntityPagingBuilder<ShadowKeyEntity>.BuildPagedQuery(context.Set<ShadowKeyEntity>(), orders, keys, 0, 10);

                Assert.Equal(5, query.ToList().Count);
                Assert.Contains("ShadowId", query.ToQueryString());
            }
        }

        [Fact]
        public void TestBuildPagedQueryOrdersByACompositeKey()
        {
            using (var context = CompositeKeyContext.CreateSeeded(rows: 6))
            {
                var keys = EntityPagingBuilder<CompositeKeyEntity>.KeyProperties(context);
                var orders = PagingRules.MergeOrders<CompositeKeyEntity>(
                    new[] { Order.Asc("Name") }, PageRequest.Of(0, 10), keys.Select(k => k.Name));

                var query = EntityPagingBuilder<CompositeKeyEntity>.BuildPagedQuery(context.Set<CompositeKeyEntity>(), orders, keys, 0, 10);

                Assert.Equal(new[] { "Name", "A", "B" }, orders.Select(o => o.PropertyName));
                Assert.Equal(6, query.ToList().Count);
            }
        }

        [Fact]
        public void TestBuildPagedQueryStillFailsForAnUnknownOrderProperty()
        {
            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.BuildPagedQuery(
                context.Set<Product>(), new[] { Order.Asc("Nope") }, [], 0, 10));
        }

        [Fact]
        public void TestBuildPagedQueryTurnsSkipAndTakeIntoSqlParameters()
        {
            var query = EntityPagingBuilder<Product>.BuildPagedQuery(
                context.Set<Product>().AsNoTracking(),
                new[] { Order.Asc("Id") }, [], skip: 20, take: 10);

            var sql = query.ToQueryString();

            Assert.Contains("OFFSET @", sql);
            Assert.DoesNotContain("OFFSET 20", sql);
        }
    }

}
