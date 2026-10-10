using System;
using System.Collections.Generic;
using System.Linq;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.EntityFrameworkCore.Query.Support;

using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Query.Support
{
    public class EntityOrderedQueryBuilderFakeEntity
    {
        public string Name { get; set; }
        public int Stock { get; set; }
    }

    public class EntityOrderedQueryBuilderTest
    {
        private readonly IQueryable<EntityOrderedQueryBuilderFakeEntity> entities = new[]
        {
            new EntityOrderedQueryBuilderFakeEntity { Name = "Barnes", Stock = 10 },
            new EntityOrderedQueryBuilderFakeEntity { Name = "Adams", Stock = 30 },
            new EntityOrderedQueryBuilderFakeEntity { Name = "Adams", Stock = 10 },
        }.AsQueryable();

        [Fact]
        public void TestOrdersAscendingBySingleProperty()
        {
            var orders = new List<IOrderCriterion> { Order.Asc("Name") };
            var result = EntityOrderedQueryBuilder<EntityOrderedQueryBuilderFakeEntity>
                .BuildOrderedQuery(entities, orders).ToList();

            Assert.Equal("Adams", result[0].Name);
            Assert.Equal("Adams", result[1].Name);
            Assert.Equal("Barnes", result[2].Name);
        }

        [Fact]
        public void TestOrdersDescendingBySingleProperty()
        {
            var orders = new List<IOrderCriterion> { Order.Desc("Stock") };
            var result = EntityOrderedQueryBuilder<EntityOrderedQueryBuilderFakeEntity>
                .BuildOrderedQuery(entities, orders).ToList();

            Assert.Equal(30, result[0].Stock);
        }

        [Fact]
        public void TestOrdersByMultiplePropertiesInCallSequence()
        {
            var orders = new List<IOrderCriterion> { Order.Asc("Name"), Order.Desc("Stock") };
            var result = EntityOrderedQueryBuilder<EntityOrderedQueryBuilderFakeEntity>
                .BuildOrderedQuery(entities, orders).ToList();

            Assert.Equal("Adams", result[0].Name);
            Assert.Equal(30, result[0].Stock);
            Assert.Equal("Adams", result[1].Name);
            Assert.Equal(10, result[1].Stock);
            Assert.Equal("Barnes", result[2].Name);
        }

        [Fact]
        public void TestReturnsSameQueryWhenThereAreNoOrders()
        {
            var result = EntityOrderedQueryBuilder<EntityOrderedQueryBuilderFakeEntity>
                .BuildOrderedQuery(entities, new List<IOrderCriterion>());

            Assert.Same(entities, result);
        }

        [Fact]
        public void TestRejectsUnknownPropertyWhenBuilding()
        {
            var orders = new List<IOrderCriterion> { Order.Asc("DoesNotExist") };

            Assert.Throws<ArgumentException>(() =>
                EntityOrderedQueryBuilder<EntityOrderedQueryBuilderFakeEntity>
                    .BuildOrderedQuery(entities, orders));
        }
    }
}
