using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Repository.Paging;
using DataQI.EntityFrameworkCore.Query.Support;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    public class EntityPagingBuilderTest
    {
        [Fact]
        public void TestMergeKeepsMethodThenCriteriaThenPageThenKeyOrderAndDropsRepeats()
        {
            var orders = EntityPagingBuilder<Product>.MergeOrders(
                new[] { Order.Asc("Name") },
                PageRequest.Of(0, 10, Order.Desc("Name"), Order.Desc("Price")),
                new[] { "Id" });

            Assert.Equal(new[] { "Name", "Price", "Id" }, orders.Select(o => o.PropertyName));
            Assert.Equal(OrderDirection.Asc, orders[0].Direction);
            Assert.Equal(OrderDirection.Asc, orders[2].Direction);
        }

        [Fact]
        public void TestMergeDoesNotRepeatAKeyAlreadyInTheOrdering()
        {
            var orders = EntityPagingBuilder<Product>.MergeOrders(
                new[] { Order.Desc("Id") }, PageRequest.Of(0, 10), new[] { "Id" });

            Assert.Single(orders);
            Assert.Equal(OrderDirection.Desc, orders[0].Direction);
        }

        [Fact]
        public void TestMergeAppendsEveryPartOfACompositeKey()
        {
            var orders = EntityPagingBuilder<Product>.MergeOrders(
                new IOrderCriterion[0], PageRequest.Of(0, 10), new[] { "A", "B" });

            Assert.Equal(new[] { "A", "B" }, orders.Select(o => o.PropertyName));
        }

        [Fact]
        public void TestMergeFailsWithoutKeyAndWithoutExplicitOrder()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                EntityPagingBuilder<Product>.MergeOrders(new IOrderCriterion[0], PageRequest.Of(0, 10), new string[0]));

            Assert.Contains(nameof(Product), exception.Message);
        }

        [Fact]
        public void TestMergeAcceptsAnEntityWithoutKeyWhenAnOrderIsGiven()
        {
            var orders = EntityPagingBuilder<Product>.MergeOrders(
                new[] { Order.Asc("Name") }, PageRequest.Of(0, 10), new string[0]);

            Assert.Single(orders);
        }

        [Fact]
        public void TestEnsurePageRejectsNull()
        {
            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.EnsurePage(null));
        }

        [Fact]
        public void TestEnsurePageRejectsOffsetBeyondIntMax()
        {
            var page = PageRequest.Of(int.MaxValue, 10);

            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.EnsurePage(page));
        }

        [Fact]
        public void TestEnsurePageAcceptsAValidCustomImplementation()
        {
            EntityPagingBuilder<Product>.EnsurePage(new CustomPageRequest(2, 10, 20, new IOrderCriterion[0]));
        }

        [Theory]
        [InlineData(-1, 10, -10L)]
        [InlineData(0, 0, 0L)]
        [InlineData(0, -3, 0L)]
        [InlineData(0, int.MaxValue, 0L)]
        [InlineData(2, 10, 5L)]
        [InlineData(2, 10, -20L)]
        public void TestEnsurePageRejectsInconsistentCustomImplementations(int index, int size, long offset)
        {
            var page = new CustomPageRequest(index, size, offset, new IOrderCriterion[0]);

            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.EnsurePage(page));
        }

        [Fact]
        public void TestEnsurePageRejectsNullOrders()
        {
            var page = new CustomPageRequest(0, 10, 0, null);

            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.EnsurePage(page));
        }

        [Fact]
        public void TestEnsurePageRejectsNullOrderOrEmptyPropertyName()
        {
            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.EnsurePage(
                new CustomPageRequest(0, 10, 0, new IOrderCriterion[] { null })));
            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.EnsurePage(
                new CustomPageRequest(0, 10, 0, new[] { Order.Asc("") })));
        }

        [Theory]
        [InlineData(null, OrderDirection.Asc)]
        [InlineData("", OrderDirection.Asc)]
        [InlineData("   ", OrderDirection.Asc)]
        [InlineData("Name", (OrderDirection)123)]
        public void TestEnsurePageRejectsInvalidCustomOrders(string name, OrderDirection direction)
        {
            var page = new CustomPageRequest(0, 10, 0, new IOrderCriterion[] { new CustomOrder(name, direction) });
            Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.EnsurePage(page));
        }

        private sealed class CustomOrder : IOrderCriterion
        {
            public CustomOrder(string name, OrderDirection direction)
            {
                PropertyName = name;
                Direction = direction;
            }
            public string PropertyName { get; }
            public OrderDirection Direction { get; }
        }

        [Fact]
        public void TestKeyPropertiesComeFromTheModel()
        {
            using (var context = TestContext.NewInstance("dbtest-paging-builder.s3db"))
            {
                Assert.Equal(new[] { nameof(Product.Id) },
                    EntityPagingBuilder<Product>.KeyProperties(context).Select(k => k.Name));
                context.Database.EnsureDeleted();
            }
        }

        [Fact]
        public void TestWindowOrdersByAShadowKey()
        {
            using (var context = ShadowKeyContext.CreateSeeded(rows: 5))
            {
                var keys = EntityPagingBuilder<ShadowKeyEntity>.KeyProperties(context);
                var orders = EntityPagingBuilder<ShadowKeyEntity>.MergeOrders(
                    new[] { Order.Asc("Name") }, PageRequest.Of(0, 10), keys.Select(k => k.Name));

                var query = EntityPagingBuilder<ShadowKeyEntity>.Window(context.Set<ShadowKeyEntity>(), orders, keys, 0, 10);

                Assert.Equal(5, query.ToList().Count);
                Assert.Contains("ShadowId", query.ToQueryString());
            }
        }

        [Fact]
        public void TestWindowOrdersByACompositeKey()
        {
            using (var context = CompositeKeyContext.CreateSeeded(rows: 6))
            {
                var keys = EntityPagingBuilder<CompositeKeyEntity>.KeyProperties(context);
                var orders = EntityPagingBuilder<CompositeKeyEntity>.MergeOrders(
                    new[] { Order.Asc("Name") }, PageRequest.Of(0, 10), keys.Select(k => k.Name));

                var query = EntityPagingBuilder<CompositeKeyEntity>.Window(context.Set<CompositeKeyEntity>(), orders, keys, 0, 10);

                Assert.Equal(new[] { "Name", "A", "B" }, orders.Select(o => o.PropertyName));
                Assert.Equal(6, query.ToList().Count);
            }
        }

        [Fact]
        public void TestWindowStillFailsForAnUnknownOrderProperty()
        {
            using (var context = TestContext.NewInstance("dbtest-paging-builder.s3db"))
            {
                Assert.Throws<ArgumentException>(() => EntityPagingBuilder<Product>.Window(
                    context.Set<Product>(), new[] { Order.Asc("Nope") }, new (string, Type)[0], 0, 10));
                context.Database.EnsureDeleted();
            }
        }

        [Theory]
        [InlineData(5, 10, 2, 25L)]
        [InlineData(0, 10, 0, 0L)]
        public void TestDeducesTheTotalWhenThePageRevealsIt(int items, int size, int index, long expected)
        {
            Assert.Equal(expected, EntityPagingBuilder<Product>.DeduceTotal(items, PageRequest.Of(index, size)));
        }

        [Theory]
        [InlineData(10, 10, 0)]
        [InlineData(0, 10, 5)]
        public void TestDoesNotDeduceTheTotalOtherwise(int items, int size, int index)
        {
            Assert.Null(EntityPagingBuilder<Product>.DeduceTotal(items, PageRequest.Of(index, size)));
        }

        [Fact]
        public void TestWindowTurnsSkipAndTakeIntoSqlParameters()
        {
            using (var context = TestContext.NewInstance("dbtest-paging-builder.s3db"))
            {
                var query = EntityPagingBuilder<Product>.Window(
                    context.Set<Product>().AsNoTracking(),
                    new[] { Order.Asc("Id") }, new (string, Type)[0], skip: 20, take: 10);

                var sql = query.ToQueryString();

                Assert.Contains("OFFSET @", sql);
                Assert.DoesNotContain("OFFSET 20", sql);
                context.Database.EnsureDeleted();
            }
        }
    }

}
