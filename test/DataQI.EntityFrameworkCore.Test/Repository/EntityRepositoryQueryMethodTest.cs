using System;
using System.Collections.Generic;
using System.Linq;

using ExpectedObjects;
using Xunit;

using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test.Repository
{
    public class EntityRepositoryQueryMethodTest : IClassFixture<DbFixture>, IDisposable
    {
        private readonly TestContext productContext;

        private readonly IProductRepository productRepository;

        public EntityRepositoryQueryMethodTest(DbFixture fixture)
        {
            productContext = fixture.ProductContext;
            productRepository = fixture.ProductRepository;
        }

        [Fact]
        public void TestFindByEanLike()
        {
            var productsExpected = InsertTestProducts();

            while (productsExpected.MoveNext())
            {
                var productExpected = productsExpected.Current;
                var products = productRepository.FindByEanLike(productExpected.Ean);

                productExpected.ToExpectedObject().ShouldMatch(products.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByIdOrEanOrReference()
        {
            var productsExpected = InsertTestProducts();

            while (productsExpected.MoveNext())
            {
                var productExpected = productsExpected.Current;

                var productById = productRepository.FindByIdOrEanOrReference(productExpected.Id, "", "");
                var productByEan = productRepository.FindByIdOrEanOrReference(0, productExpected.Ean, "");
                var productByReference = productRepository.FindByIdOrEanOrReference(0, "", productExpected.Reference);

                productExpected.ToExpectedObject().ShouldMatch(productById.FirstOrDefault());
                productExpected.ToExpectedObject().ShouldMatch(productByEan.FirstOrDefault());
                productExpected.ToExpectedObject().ShouldMatch(productByReference.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByDepartamentInAndNameLike()
        {
            var productList = InsertTestProductsList();

            var departments = productList.Select(p => p.Department);
            var productEnumerator = productList.GetEnumerator();

            while (productEnumerator.MoveNext())
            {
                var product = productEnumerator.Current;
                var productsExpected = productList.Where(p =>
                    departments.Any(d => d == p.Department) &&
                    p.Name.Substring(1, 10) == product.Name.Substring(1, 10));
                var products = productRepository.FindByDepartmentInAndNameLike(departments.ToArray(), product.Name);

                productsExpected.ToExpectedObject().ShouldMatch(products);
            }
        }

        [Fact]
        public void TestFindByKeywordsLikeAndActive()
        {
            var productList = InsertTestProductsList();
            var productEnumerator = productList.GetEnumerator();

            while (productEnumerator.MoveNext())
            {
                var product = productEnumerator.Current;
                var productsExpected = productList.Where(p =>
                    p.Active == product.Active && p.Keywords.Contains(product.Keywords));
                var products = productRepository.FindByKeywordsLikeAndActive(product.Keywords, product.Active);

                productsExpected.ToExpectedObject().ShouldMatch(products);
            }
        }

        private IEnumerator<Product> InsertTestProducts()
        {
            var Products = InsertTestProductsList();
            return Products.GetEnumerator();
        }

        private IList<Product> InsertTestProductsList()
        {
            var Products = new List<Product>()
            {
                ProductBuilder.NewInstance().Build(),
                ProductBuilder.NewInstance().Build(),
                ProductBuilder.NewInstance().Build(),
                ProductBuilder.NewInstance().Build(),
                ProductBuilder.NewInstance().Build(),
            };

            Products.ForEach(p =>
            {
                productRepository.Save(p);
                productContext.SaveChanges();

                Assert.True(productRepository.Exists(p.Id));
            });

            return Products;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                productContext.ClearProducts();
                productContext.SaveChanges();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
