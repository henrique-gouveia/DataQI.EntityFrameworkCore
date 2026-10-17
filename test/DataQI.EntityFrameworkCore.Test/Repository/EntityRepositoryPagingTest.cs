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
using DataQI.Commons.Repository.Paging.Support;

using DataQI.EntityFrameworkCore.Repository.Support;
using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Fixtures.Paging;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test.Repository
{
    [Collection(nameof(DatabaseCollection))]
    public sealed class EntityRepositoryPagingTest : IClassFixture<DbFixture>, IDisposable
    {
        private readonly TestContext productContext;
        private readonly IProductRepository productRepository;
        private readonly EntityRepository<Product, int> directRepository;

        public EntityRepositoryPagingTest(DbFixture fixture)
        {
            productContext = fixture.ProductContext;
            productRepository = fixture.ProductRepository;
            directRepository = new EntityRepository<Product, int>(productContext);
            ReplaceProducts(25);
        }

        [Theory]
        [InlineData(0, 10, true, false)]
        [InlineData(2, 5, false, true)]
        [InlineData(5, 0, false, true)]
        public void TestFindSliceReturnsRequestedPage(int pageIndex, int itemCount, bool hasNext, bool hasPrevious)
        {
            var request = PageRequest.Of(pageIndex, 10);

            var slice = productRepository.FindSlice(c => c, request);

            Assert.Equal(itemCount, slice.Items.Count);
            Assert.Equal(hasNext, slice.HasNext);
            Assert.Equal(hasPrevious, slice.HasPrevious);
        }

        [Theory]
        [InlineData(0, 10, true, false)]
        [InlineData(2, 5, false, true)]
        [InlineData(5, 0, false, true)]
        public async Task TestFindSliceAsyncReturnsRequestedPage(int pageIndex, int itemCount, bool hasNext, bool hasPrevious)
        {
            var request = PageRequest.Of(pageIndex, 10);

            var slice = await productRepository.FindSliceAsync(c => c, request);

            Assert.Equal(itemCount, slice.Items.Count);
            Assert.Equal(hasNext, slice.HasNext);
            Assert.Equal(hasPrevious, slice.HasPrevious);
        }

        [Theory]
        [InlineData(0, 10, true)]
        [InlineData(2, 5, false)]
        [InlineData(5, 0, false)]
        public void TestFindPageReturnsRequestedPage(int pageIndex, int itemCount, bool hasNext)
        {
            var request = PageRequest.Of(pageIndex, 10);

            var page = productRepository.FindPage(c => c, request);

            Assert.Equal(itemCount, page.Items.Count);
            Assert.Equal(hasNext, page.HasNext);
        }

        [Theory]
        [InlineData(0, 10, true)]
        [InlineData(2, 5, false)]
        [InlineData(5, 0, false)]
        public async Task TestFindPageAsyncReturnsRequestedPage(int pageIndex, int itemCount, bool hasNext)
        {
            var request = PageRequest.Of(pageIndex, 10);

            var page = await productRepository.FindPageAsync(c => c, request);

            Assert.Equal(itemCount, page.Items.Count);
            Assert.Equal(hasNext, page.HasNext);
        }

        [Fact]
        public void TestFindSliceHasNoNextPageWhenLastPageIsFull()
        {
            ReplaceProducts(20);

            var slice = productRepository.FindSlice(c => c, PageRequest.Of(1, 10));

            Assert.Equal(10, slice.Items.Count);
            Assert.False(slice.HasNext);
        }

        [Fact]
        public async Task TestFindSliceAsyncHasNoNextPageWhenLastPageIsFull()
        {
            ReplaceProducts(20);

            var slice = await productRepository.FindSliceAsync(c => c, PageRequest.Of(1, 10));

            Assert.Equal(10, slice.Items.Count);
            Assert.False(slice.HasNext);
        }

        [Fact]
        public void TestFindPageHasNoNextPageWhenLastPageIsFull()
        {
            ReplaceProducts(20);

            var page = productRepository.FindPage(c => c, PageRequest.Of(1, 10));

            Assert.Equal(10, page.Items.Count);
            Assert.False(page.HasNext);
            Assert.Equal(20, page.TotalCount);
        }

        [Fact]
        public async Task TestFindPageAsyncHasNoNextPageWhenLastPageIsFull()
        {
            ReplaceProducts(20);

            var page = await productRepository.FindPageAsync(c => c, PageRequest.Of(1, 10));

            Assert.Equal(10, page.Items.Count);
            Assert.False(page.HasNext);
            Assert.Equal(20, page.TotalCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        public void TestFindPageReportsTotalRegardlessOfPageIndex(int pageIndex)
        {
            var page = productRepository.FindPage(c => c, PageRequest.Of(pageIndex, 10));

            Assert.Equal(25, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        public async Task TestFindPageAsyncReportsTotalRegardlessOfPageIndex(int pageIndex)
        {
            var page = await productRepository.FindPageAsync(c => c, PageRequest.Of(pageIndex, 10));

            Assert.Equal(25, page.TotalCount);
            Assert.Equal(3, page.TotalPages);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        public void TestFindPageTotalCountsOnlyMatchingProducts(int pageIndex)
        {
            InsertProducts(12, "D2");
            var request = PageRequest.Of(pageIndex, 10);

            var page = productRepository.FindPage(c => c.Add(Restrictions.Equal(nameof(Product.Department), "D1")), request);

            Assert.Equal(25, page.TotalCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        public async Task TestFindPageAsyncTotalCountsOnlyMatchingProducts(int pageIndex)
        {
            InsertProducts(12, "D2");
            var request = PageRequest.Of(pageIndex, 10);

            var page = await productRepository.FindPageAsync(c => c.Add(Restrictions.Equal(nameof(Product.Department), "D1")), request);

            Assert.Equal(25, page.TotalCount);
        }

        [Fact]
        public void TestFindPageReturnsEmptyResultWhenNoProductsMatch()
        {
            var request = PageRequest.Of(0, 10);

            var page = productRepository.FindPage(c => c.Add(Restrictions.Equal(nameof(Product.Department), "Missing")), request);

            Assert.Empty(page.Items);
            Assert.Equal(0, page.TotalCount);
            Assert.Equal(0, page.TotalPages);
        }

        [Fact]
        public async Task TestFindPageAsyncReturnsEmptyResultWhenNoProductsMatch()
        {
            var request = PageRequest.Of(0, 10);

            var page = await productRepository.FindPageAsync(c => c.Add(Restrictions.Equal(nameof(Product.Department), "Missing")), request);

            Assert.Empty(page.Items);
            Assert.Equal(0, page.TotalCount);
            Assert.Equal(0, page.TotalPages);
        }

        [Fact]
        public void TestFindPageWithSizeOneReturnsEveryProductOnce()
        {
            var expectedIds = productContext.Products.OrderBy(p => p.Id).Select(p => p.Id).ToArray();

            var pages = Enumerable.Range(0, 25)
                .Select(index => productRepository.FindPage(c => c, PageRequest.Of(index, 1)))
                .ToArray();

            Assert.Equal(expectedIds, pages.Select(page => Assert.Single(page.Items).Id));
            Assert.All(pages, page => Assert.Equal(25, page.TotalPages));
        }

        [Fact]
        public async Task TestFindPageAsyncWithSizeOneReturnsEveryProductOnce()
        {
            var expectedIds = productContext.Products.OrderBy(p => p.Id).Select(p => p.Id).ToArray();
            List<IPage<Product>> pages = [];

            for (var index = 0; index < 25; index++)
                pages.Add(await productRepository.FindPageAsync(c => c, PageRequest.Of(index, 1)));

            Assert.Equal(expectedIds, pages.Select(page => Assert.Single(page.Items).Id));
            Assert.All(pages, page => Assert.Equal(25, page.TotalPages));
        }

        [Fact]
        public void TestFindSliceUsesPrimaryKeyToBreakOrderingTies()
        {
            var expectedIds = productContext.Products.OrderBy(p => p.Name).ThenBy(p => p.Id).Select(p => p.Id).ToArray();

            var actualIds = Enumerable.Range(0, 3)
                .SelectMany(index => productRepository.FindSlice(c => c,
                    PageRequest.Of(index, 10, Order.Asc(nameof(Product.Name)))).Items)
                .Select(p => p.Id);

            Assert.Equal(expectedIds, actualIds);
        }

        [Fact]
        public void TestQueryMethodOrderingTakesPrecedenceOverRequestOrdering()
        {
            var request = PageRequest.Of(0, 25, Order.Desc(nameof(Product.Name)), Order.Desc(nameof(Product.Ean)));
            var expectedIds = productContext.Products.OrderBy(p => p.Name).ThenByDescending(p => p.Ean).Select(p => p.Id).ToArray();

            var slice = productRepository.FindByDepartmentOrderByNameAsc("D1", request);

            Assert.Equal(expectedIds, slice.Items.Select(p => p.Id));
        }

        [Fact]
        public void TestFindPageWithEmptyCriteriaReturnsAllProducts()
        {
            var page = productRepository.FindPage(c => c, PageRequest.Of(0, 30));

            Assert.Equal(25, page.Items.Count);
            Assert.Equal(25, page.TotalCount);
        }

        [Theory]
        [MemberData(nameof(InvalidPageRequests))]
        public void TestFindSliceRejectsInvalidPageRequest(IPageRequest request)
        {
            Assert.Throws<ArgumentException>(() => directRepository.FindSlice(c => c, request));
        }

        [Theory]
        [MemberData(nameof(InvalidPageRequests))]
        public async Task TestFindSliceAsyncRejectsInvalidPageRequest(IPageRequest request)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => directRepository.FindSliceAsync(c => c, request));
        }

        [Theory]
        [MemberData(nameof(InvalidPageRequests))]
        public void TestFindPageRejectsInvalidPageRequest(IPageRequest request)
        {
            Assert.Throws<ArgumentException>(() => directRepository.FindPage(c => c, request));
        }

        [Theory]
        [MemberData(nameof(InvalidPageRequests))]
        public async Task TestFindPageAsyncRejectsInvalidPageRequest(IPageRequest request)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => directRepository.FindPageAsync(c => c, request));
        }

        [Fact]
        public void TestQueryMethodRejectsNullPageRequest()
        {
            var exception = Assert.Throws<TargetInvocationException>(() =>
                productRepository.FindByDepartmentOrderByNameAsc("D1", null));

            Assert.IsType<ArgumentException>(exception.GetBaseException());
        }

        [Fact]
        public void TestQueryMethodRejectsOffsetBeyondIntMax()
        {
            var request = PageRequest.Of(int.MaxValue, 10);

            var exception = Assert.Throws<TargetInvocationException>(() =>
                productRepository.FindByDepartmentOrderByNameAsc("D1", request));

            Assert.IsType<ArgumentException>(exception.GetBaseException());
        }

        [Theory]
        [InlineData("Nope")]
        public void TestFindSliceRejectsUnknownOrderProperty(string propertyName)
        {
            var request = PageRequest.Of(0, 10, Order.Asc(propertyName));

            var exception = Assert.Throws<InvalidOperationException>(() => directRepository.FindSlice(c => c, request));

            Assert.Equal($"Cannot order Product by unknown property '{propertyName}'.", exception.Message);
        }

        [Theory]
        [InlineData("Nope")]
        public async Task TestFindSliceAsyncRejectsUnknownOrderProperty(string propertyName)
        {
            var request = PageRequest.Of(0, 10, Order.Asc(propertyName));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => directRepository.FindSliceAsync(c => c, request));

            Assert.Equal($"Cannot order Product by unknown property '{propertyName}'.", exception.Message);
        }

        [Theory]
        [InlineData("Nope")]
        public void TestFindPageRejectsUnknownOrderProperty(string propertyName)
        {
            var request = PageRequest.Of(0, 10, Order.Asc(propertyName));

            var exception = Assert.Throws<InvalidOperationException>(() => directRepository.FindPage(c => c, request));

            Assert.Equal($"Cannot order Product by unknown property '{propertyName}'.", exception.Message);
        }

        [Theory]
        [InlineData("Nope")]
        public async Task TestFindPageAsyncRejectsUnknownOrderProperty(string propertyName)
        {
            var request = PageRequest.Of(0, 10, Order.Asc(propertyName));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => directRepository.FindPageAsync(c => c, request));

            Assert.Equal($"Cannot order Product by unknown property '{propertyName}'.", exception.Message);
        }

        [Fact]
        public async Task TestQueryMethodWithTokenReturnsTheSamePageAsCriteria()
        {
            var request = PageRequest.Of(1, 10, Order.Desc(nameof(Product.Ean)));
            var expected = await productRepository.FindPageAsync(
                c => c.Add(Restrictions.Equal(nameof(Product.Department), "D1")), request);

            var actual = await productRepository.FindByDepartmentAsync("D1", request, CancellationToken.None);

            Assert.Equal(expected.Items.Select(p => p.Id), actual.Items.Select(p => p.Id));
        }

        [Fact]
        public async Task TestFindSliceAsyncObservesCancelledToken()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            var request = PageRequest.Of(0, 10);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                productRepository.FindSliceAsync(c => c, request, source.Token));
        }

        [Fact]
        public async Task TestFindPageAsyncObservesCancelledToken()
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            var request = PageRequest.Of(0, 10);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                productRepository.FindPageAsync(c => c, request, source.Token));
        }

        [Fact]
        public void TestFindSliceRunsOneCommand()
        {
            var counter = new CommandCounter();
            using var context = TestContext.NewInstance(interceptor: counter);
            var repository = new EntityRepository<Product, int>(context);
            counter.Count = 0;

            repository.FindSlice(c => c, PageRequest.Of(0, 10));

            Assert.Equal(1, counter.Count);
        }

        [Fact]
        public async Task TestFindSliceAsyncRunsOneCommand()
        {
            var counter = new CommandCounter();
            using var context = TestContext.NewInstance(interceptor: counter);
            var repository = new EntityRepository<Product, int>(context);
            counter.Count = 0;

            await repository.FindSliceAsync(c => c, PageRequest.Of(0, 10));

            Assert.Equal(1, counter.Count);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(2, 1)]
        [InlineData(5, 2)]
        public void TestFindPageCountsOnlyWhenTotalCannotBeDeduced(int pageIndex, int commandCount)
        {
            var counter = new CommandCounter();
            using var context = TestContext.NewInstance(interceptor: counter);
            var repository = new EntityRepository<Product, int>(context);
            var request = PageRequest.Of(pageIndex, 10);
            counter.Count = 0;

            var page = repository.FindPage(c => c, request);

            Assert.Equal(25, page.TotalCount);
            Assert.Equal(commandCount, counter.Count);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(2, 1)]
        [InlineData(5, 2)]
        public async Task TestFindPageAsyncCountsOnlyWhenTotalCannotBeDeduced(int pageIndex, int commandCount)
        {
            var counter = new CommandCounter();
            using var context = TestContext.NewInstance(interceptor: counter);
            var repository = new EntityRepository<Product, int>(context);
            var request = PageRequest.Of(pageIndex, 10);
            counter.Count = 0;

            var page = await repository.FindPageAsync(c => c, request);

            Assert.Equal(25, page.TotalCount);
            Assert.Equal(commandCount, counter.Count);
        }

        [Fact]
        public void TestUnknownOrderIsRejectedBeforeExecutingSql()
        {
            var counter = new CommandCounter();
            using var context = TestContext.NewInstance(interceptor: counter);
            var repository = new EntityRepository<Product, int>(context);
            var request = PageRequest.Of(0, 10, Order.Asc("Nope"));
            counter.Count = 0;

            Assert.Throws<InvalidOperationException>(() => repository.FindSlice(c => c, request));

            Assert.Equal(0, counter.Count);
        }

        [Fact]
        public void TestFindSliceUsesShadowKeyToBreakOrderingTies()
        {
            using var context = ShadowKeyContext.CreateSeeded(25);
            var repository = new EntityRepository<ShadowKeyEntity, int>(context);
            var expectedMarkers = Enumerable.Range(0, 25);

            var actualMarkers = Enumerable.Range(0, 3)
                .SelectMany(index => repository.FindSlice(c => c, PageRequest.Of(index, 10)).Items)
                .Select(entity => entity.Marker);

            Assert.Equal(expectedMarkers, actualMarkers);
        }

        [Fact]
        public async Task TestFindPageAsyncUsesShadowKeyToBreakOrderingTies()
        {
            using var context = ShadowKeyContext.CreateSeeded(25);
            var repository = new EntityRepository<ShadowKeyEntity, int>(context);
            var expectedMarkers = Enumerable.Range(0, 25);
            List<int> actualMarkers = [];

            for (var index = 0; index < 3; index++)
            {
                var page = await repository.FindPageAsync(c => c, PageRequest.Of(index, 10));
                actualMarkers.AddRange(page.Items.Select(entity => entity.Marker));
            }

            Assert.Equal(expectedMarkers, actualMarkers);
        }

        [Fact]
        public void TestFindSliceUsesEveryCompositeKeyPropertyToBreakOrderingTies()
        {
            using var context = CompositeKeyContext.CreateSeeded(25);
            var repository = new EntityRepository<CompositeKeyEntity, int>(context);
            var expectedKeys = Enumerable.Range(0, 25).Select(index => (index / 2, index % 2));

            var actualKeys = Enumerable.Range(0, 3)
                .SelectMany(index => repository.FindSlice(c => c,
                    PageRequest.Of(index, 10, Order.Asc(nameof(CompositeKeyEntity.Name)))).Items)
                .Select(entity => (entity.A, entity.B));

            Assert.Equal(expectedKeys, actualKeys);
        }

        [Fact]
        public async Task TestFindPageAsyncUsesEveryCompositeKeyPropertyToBreakOrderingTies()
        {
            using var context = CompositeKeyContext.CreateSeeded(25);
            var repository = new EntityRepository<CompositeKeyEntity, int>(context);
            var expectedKeys = Enumerable.Range(0, 25).Select(index => (index / 2, index % 2));
            List<(int, int)> actualKeys = [];

            for (var index = 0; index < 3; index++)
            {
                var request = PageRequest.Of(index, 10, Order.Asc(nameof(CompositeKeyEntity.Name)));
                var page = await repository.FindPageAsync(c => c, request);
                actualKeys.AddRange(page.Items.Select(entity => (entity.A, entity.B)));
            }

            Assert.Equal(expectedKeys, actualKeys);
        }

        [Fact]
        public void TestFindSliceWithoutKeyRequiresExplicitOrdering()
        {
            using var context = new KeylessContext();
            var repository = new EntityRepository<KeylessEntity, int>(context);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                repository.FindSlice(c => c, PageRequest.Of(0, 10)));

            Assert.Equal("Cannot page KeylessEntity: it has no primary key and no explicit ordering", exception.Message);
        }

        public static IEnumerable<object[]> InvalidPageRequests()
        {
            yield return [null];
            yield return [PageRequest.Of(int.MaxValue, 10)];
            yield return [new CustomPageRequest(0, 0, 0, [])];
            yield return [new CustomPageRequest(2, 10, 5, [])];
        }

        public void Dispose()
        {
            productContext.ClearProducts();
            productContext.SaveChanges();
            productContext.ChangeTracker.Clear();
        }

        private void ReplaceProducts(int count)
        {
            productContext.ClearProducts();
            productContext.SaveChanges();
            productContext.ChangeTracker.Clear();
            InsertProducts(count);
        }

        private void InsertProducts(int count, string department = "D1")
        {
            for (var index = 0; index < count; index++)
            {
                productContext.Add(new Product
                {
                    Name = "Name" + index % 5,
                    Ean = department + index.ToString("D4"),
                    Department = department
                });
            }

            productContext.SaveChanges();
            productContext.ChangeTracker.Clear();
        }

        private sealed class CustomPageRequest : IPageRequest
        {
            public CustomPageRequest(int pageIndex, int pageSize, long offset, IReadOnlyList<IOrderCriterion> orders)
            {
                PageIndex = pageIndex;
                PageSize = pageSize;
                Offset = offset;
                Orders = orders;
            }

            public int PageIndex { get; }
            public int PageSize { get; }
            public long Offset { get; }
            public IReadOnlyList<IOrderCriterion> Orders { get; }
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
