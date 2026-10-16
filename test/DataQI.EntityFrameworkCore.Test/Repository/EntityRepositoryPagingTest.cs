using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;
using DataQI.Commons.Repository.Paging;
using DataQI.EntityFrameworkCore.Repository.Support;
using DataQI.EntityFrameworkCore.Test.Repository.Products;
using DataQI.EntityFrameworkCore.Test.Query.Support;

namespace DataQI.EntityFrameworkCore.Test.Repository
{
    public class EntityRepositoryPagingTest : IDisposable
    {
        private readonly TestContext context;
        private readonly IProductRepository repository;
        private readonly EntityRepository<Product, int> direct;

        public EntityRepositoryPagingTest()
        {
            context = TestContext.NewInstance("dbtest-paging.s3db");
            repository = new EntityRepositoryFactory().GetRepository<IProductRepository>(context);
            direct = new EntityRepository<Product, int>(context);
            Seed(25);
        }

        private void Seed(int count, string department = "D1")
        {
            for (var i = 0; i < count; i++)
                context.Add(new Product { Name = "Name" + (i % 5), Ean = i.ToString("D4"), Department = department });
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        private static ICriteria Department(ICriteria criteria) => criteria.Add(Restrictions.Equal("Department", "D1"));

        [Fact]
        public void TestFirstPage()
        {
            var slice = repository.FindSlice(c => c, PageRequest.Of(0, 10));
            Assert.Equal(10, slice.Items.Count);
            Assert.True(slice.HasNext);
            Assert.False(slice.HasPrevious);
            var page = repository.FindPage(c => c, PageRequest.Of(0, 10));
            Assert.Equal(25, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
        }

        [Fact]
        public async Task TestLastPartialPage()
        {
            var slice = repository.FindSlice(c => c, PageRequest.Of(2, 10));
            Assert.Equal(5, slice.Items.Count);
            Assert.False(slice.HasNext);
            var page = repository.FindPage(c => c, PageRequest.Of(2, 10));
            Assert.Equal(5, page.Items.Count);
            Assert.False(page.HasNext);
            Assert.Equal(25, page.TotalCount);
            Assert.Equal(25, (await repository.FindPageAsync(c => c, PageRequest.Of(2, 10))).TotalCount);
        }

        [Fact]
        public void TestExactMultipleLastPage()
        {
            context.ClearProducts();
            context.SaveChanges();
            Seed(20);
            var slice = repository.FindSlice(c => c, PageRequest.Of(1, 10));
            var page = repository.FindPage(c => c, PageRequest.Of(1, 10));
            Assert.Equal(10, slice.Items.Count);
            Assert.False(slice.HasNext);
            Assert.Equal(10, page.Items.Count);
            Assert.False(page.HasNext);
        }

        [Fact]
        public async Task TestSliceAsyncFirstAndLastPage()
        {
            var first = await repository.FindSliceAsync(c => c, PageRequest.Of(0, 10));
            Assert.Equal(10, first.Items.Count);
            Assert.True(first.HasNext);
            Assert.False(first.HasPrevious);
            var last = await repository.FindSliceAsync(c => c, PageRequest.Of(2, 10));
            Assert.Equal(5, last.Items.Count);
            Assert.False(last.HasNext);
            Assert.True(last.HasPrevious);
            var beyond = await repository.FindSliceAsync(c => c, PageRequest.Of(5, 10));
            Assert.Empty(beyond.Items);
            Assert.False(beyond.HasNext);
            context.ClearProducts();
            context.SaveChanges();
            Seed(20);
            var exact = await repository.FindSliceAsync(c => c, PageRequest.Of(1, 10));
            Assert.Equal(10, exact.Items.Count);
            Assert.False(exact.HasNext);
        }

        [Fact]
        public void TestPageBeyondTheEnd()
        {
            var page = repository.FindPage(c => c, PageRequest.Of(5, 10));
            Assert.Empty(page.Items);
            Assert.Equal(25, page.TotalCount);
        }

        [Fact]
        public async Task TestPageTotalRespectsTheCriteria()
        {
            Seed(12, "D2");
            foreach (var index in new[] { 0, 5 })
            {
                Assert.Equal(25, repository.FindPage(Department, PageRequest.Of(index, 10)).TotalCount);
                Assert.Equal(25, (await repository.FindPageAsync(Department, PageRequest.Of(index, 10))).TotalCount);
            }
        }

        [Fact]
        public async Task TestFilterWithoutResults()
        {
            var page = repository.FindPage(c => c.Add(Restrictions.Equal("Department", "Missing")), PageRequest.Of(0, 10));
            Assert.Empty(page.Items);
            Assert.Equal(0, page.TotalCount);
            Assert.Equal(0, page.TotalPages);
            Assert.Equal(0, (await repository.FindPageAsync(c => c.Add(Restrictions.Equal("Department", "Missing")), PageRequest.Of(0, 10))).TotalCount);
        }

        [Fact]
        public void TestPageSizeOne()
        {
            var ids = new HashSet<int>();
            for (var i = 0; i < 25; i++)
            {
                var page = repository.FindPage(c => c, PageRequest.Of(i, 1));
                Assert.Equal(25, page.TotalPages);
                Assert.True(ids.Add(Assert.Single(page.Items).Id));
            }
        }

        [Fact]
        public void TestRepeatedNamesWithOrderByNameCoverAllRowsOnce()
        {
            var ids = Enumerable.Range(0, 3).SelectMany(i => repository.FindSlice(c => c,
                PageRequest.Of(i, 10, Order.Asc("Name"))).Items).Select(p => p.Id).ToArray();
            Assert.Equal(25, ids.Length);
            Assert.Equal(25, ids.Distinct().Count());
            Assert.Equal(context.Products.OrderBy(p => p.Name).ThenBy(p => p.Id).Select(p => p.Id), ids);
        }

        [Fact]
        public void TestMethodNameOrderThenPageOrders()
        {
            var result = repository.FindByDepartmentOrderByNameAsc("D1",
                PageRequest.Of(0, 25, Order.Desc("Name"), Order.Desc("Ean")));
            Assert.Equal(context.Products.OrderBy(p => p.Name).ThenByDescending(p => p.Ean).Select(p => p.Id),
                result.Items.Select(p => p.Id));
        }

        [Fact]
        public void TestEmptyCriteriaPagesWholeTable()
        {
            Assert.Equal(25, repository.FindPage(c => c, PageRequest.Of(0, 30)).Items.Count);
        }

        [Fact]
        public async Task TestNullPageRequestFailsOnTheRepository()
        {
            Assert.Throws<ArgumentException>(() => direct.FindSlice(c => c, null));
            Assert.Throws<ArgumentException>(() => direct.FindPage(c => c, null));
            await Assert.ThrowsAsync<ArgumentException>(() => direct.FindSliceAsync(c => c, null));
            await Assert.ThrowsAsync<ArgumentException>(() => direct.FindPageAsync(c => c, null));
        }

        [Fact]
        public void TestNullPageRequestFailsThroughTheProxy()
        {
            var exception = Assert.Throws<TargetInvocationException>(() => repository.FindByDepartmentOrderByNameAsc("D1", null));
            Assert.IsType<ArgumentException>(exception.GetBaseException());
        }

        [Fact]
        public async Task TestOffsetBeyondIntMaxIsRejectedOnTheRepository()
        {
            var page = PageRequest.Of(int.MaxValue, 10);
            Assert.Throws<ArgumentException>(() => direct.FindSlice(c => c, page));
            Assert.Throws<ArgumentException>(() => direct.FindPage(c => c, page));
            await Assert.ThrowsAsync<ArgumentException>(() => direct.FindSliceAsync(c => c, page));
            await Assert.ThrowsAsync<ArgumentException>(() => direct.FindPageAsync(c => c, page));
        }

        [Fact]
        public void TestOffsetBeyondIntMaxIsRejectedThroughTheProxy()
        {
            var exception = Assert.Throws<TargetInvocationException>(() =>
                repository.FindByDepartmentOrderByNameAsc("D1", PageRequest.Of(int.MaxValue, 10)));
            Assert.IsType<ArgumentException>(exception.GetBaseException());
        }

        [Fact]
        public void TestCustomPageRequestWithInvalidValuesIsRejected()
        {
            foreach (var page in new[] { new CustomPageRequest(0, 0, 0, Array.Empty<IOrderCriterion>()),
                new CustomPageRequest(2, 10, 5, Array.Empty<IOrderCriterion>()) })
            {
                Assert.Throws<ArgumentException>(() => direct.FindSlice(c => c, page));
                Assert.Throws<ArgumentException>(() => direct.FindPage(c => c, page));
            }
        }

        [Fact]
        public void TestUnknownOrderPropertyFails()
        {
            Assert.Throws<TargetInvocationException>(() => repository.FindSlice(c => c, PageRequest.Of(0, 10, Order.Asc("Nope"))));
        }

        [Fact]
        public async Task TestQueryMethodWithPageRequestAndTokenMatchesFindSlice()
        {
            var page = PageRequest.Of(1, 10);
            var actual = await repository.FindByDepartmentAsync("D1", page, CancellationToken.None);
            var expected = await repository.FindPageAsync(Department, page);
            Assert.Equal(expected.Items.Select(p => p.Id), actual.Items.Select(p => p.Id));
        }

        [Fact]
        public async Task TestCancelledTokenIsObserved()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.FindSliceAsync(c => c, PageRequest.Of(0, 10), source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.FindPageAsync(c => c, PageRequest.Of(0, 10), source.Token));
        }

        [Fact]
        public async Task TestSliceRunsOneCommandAndPageDeducesTheTotal()
        {
            var counter = new CommandCounter();
            using var counted = TestContext.NewInstance("dbtest-paging-commands.s3db", counter);
            try
            {
                counted.Products.AddRange(context.Products.AsNoTracking().ToList());
                counted.SaveChanges();
                var repo = new EntityRepository<Product, int>(counted);
                counter.Count = 0;
                repo.FindSlice(c => c, PageRequest.Of(0, 10));
                Assert.Equal(1, counter.Count);
                counter.Count = 0;
                Assert.Equal(25, repo.FindPage(c => c, PageRequest.Of(2, 10)).TotalCount);
                Assert.Equal(1, counter.Count);
                counter.Count = 0;
                Assert.Equal(25, repo.FindPage(c => c, PageRequest.Of(0, 10)).TotalCount);
                Assert.Equal(2, counter.Count);
                counter.Count = 0;
                await repo.FindSliceAsync(c => c, PageRequest.Of(0, 10));
                Assert.Equal(1, counter.Count);
                counter.Count = 0;
                await repo.FindPageAsync(c => c, PageRequest.Of(2, 10));
                Assert.Equal(1, counter.Count);
                counter.Count = 0;
                await repo.FindPageAsync(c => c, PageRequest.Of(0, 10));
                Assert.Equal(2, counter.Count);
                counter.Count = 0;
                Assert.Throws<ArgumentException>(() => repo.FindSlice(c => c, PageRequest.Of(0, 10, Order.Asc("Nope"))));
                Assert.Equal(0, counter.Count);
            }
            finally
            {
                counted.Database.EnsureDeleted();
            }
        }

        [Fact]
        public async Task TestShadowKeyPagesCoverAllRowsOnce()
        {
            using var shadow = ShadowKeyContext.CreateSeeded(25);
            var repo = new EntityRepository<ShadowKeyEntity, int>(shadow);
            var sync = Enumerable.Range(0, 3).SelectMany(i => repo.FindSlice(c => c, PageRequest.Of(i, 10)).Items).ToArray();
            var asyncRows = new List<ShadowKeyEntity>();
            for (var i = 0; i < 3; i++)
                asyncRows.AddRange((await repo.FindPageAsync(c => c, PageRequest.Of(i, 10))).Items);
            Assert.Equal(25, sync.Length);
            Assert.Equal(25, asyncRows.Count);
            Assert.Equal(25, sync.Select(e => e.Marker).Distinct().Count());
            Assert.Equal(sync.Select(e => e.Marker), asyncRows.Select(e => e.Marker));
        }

        [Fact]
        public async Task TestCompositeKeyPagesCoverAllRowsOnce()
        {
            using var composite = CompositeKeyContext.CreateSeeded(25);
            var repo = new EntityRepository<CompositeKeyEntity, int>(composite);
            var sync = Enumerable.Range(0, 3).SelectMany(i => repo.FindSlice(c => c, PageRequest.Of(i, 10, Order.Asc("Name"))).Items)
                .Select(e => (e.A, e.B)).ToArray();
            var asyncKeys = new List<(int, int)>();
            for (var i = 0; i < 3; i++)
                asyncKeys.AddRange((await repo.FindPageAsync(c => c, PageRequest.Of(i, 10))).Items.Select(e => (e.A, e.B)));
            Assert.Equal(25, sync.Distinct().Count());
            Assert.Equal(sync, asyncKeys);
        }

        [Fact]
        public void TestKeylessEntityNeedsExplicitOrdering()
        {
            using var keyless = new KeylessContext();
            var repo = new EntityRepository<KeylessEntity, int>(keyless);
            Assert.Empty(DataQI.EntityFrameworkCore.Query.Support.EntityPagingBuilder<KeylessEntity>.KeyProperties(keyless));
            var exception = Assert.Throws<InvalidOperationException>(() => repo.FindSlice(c => c, PageRequest.Of(0, 10)));
            Assert.Contains(nameof(KeylessEntity), exception.Message);
        }

        private sealed class KeylessEntity
        {
            public string Name { get; set; }
        }

        private sealed class KeylessContext : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlite("Data Source=:memory:");
            protected override void OnModelCreating(ModelBuilder modelBuilder)
                => modelBuilder.Entity<KeylessEntity>().HasNoKey();
        }

        public void Dispose()
        {
            context.Database.EnsureDeleted();
            context.Dispose();
        }

        private sealed class CommandCounter : DbCommandInterceptor
        {
            public int Count { get; set; }
            public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
                InterceptionResult<DbDataReader> result)
            {
                Count++;
                return result;
            }
            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
                CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                Count++;
                return new ValueTask<InterceptionResult<DbDataReader>>(result);
            }
        }
    }
}
