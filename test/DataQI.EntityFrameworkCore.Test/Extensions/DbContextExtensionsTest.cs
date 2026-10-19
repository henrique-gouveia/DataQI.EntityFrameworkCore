using System.Linq;

using Xunit;

using DataQI.EntityFrameworkCore.Extensions;
using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Fixtures.Paging;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test.Extensions
{
    [Collection(nameof(DatabaseCollection))]
    public sealed class DbContextExtensionsTest : IClassFixture<DbFixture>
    {
        private readonly TestContext context;

        public DbContextExtensionsTest(DbFixture fixture)
        {
            context = fixture.ProductContext;
        }

        [Fact]
        public void TestKeyPropertiesComeFromTheModel()
        {
            Assert.Equal(new[] { nameof(Product.Id) },
                context.KeyProperties<Product>().Select(k => k.Name));
        }

        [Fact]
        public void TestKeyPropertiesIncludeShadowKey()
        {
            using var shadowContext = ShadowKeyContext.CreateSeeded(rows: 0);

            var keys = shadowContext.KeyProperties<ShadowKeyEntity>();

            Assert.Equal(new[] { ("ShadowId", typeof(int)) }, keys);
        }

        [Fact]
        public void TestKeyPropertiesIncludeEveryCompositeKeyProperty()
        {
            using var compositeContext = CompositeKeyContext.CreateSeeded(rows: 0);

            var keys = compositeContext.KeyProperties<CompositeKeyEntity>();

            Assert.Equal(new[] { (nameof(CompositeKeyEntity.A), typeof(int)), (nameof(CompositeKeyEntity.B), typeof(int)) }, keys);
        }

        [Fact]
        public void TestKeyOfReadsTheValueOfASingleColumnKey()
        {
            var product = new Product { Id = 7 };

            Assert.Equal(7, context.KeyOf<Product, int>(product));
        }

        [Fact]
        public void TestKeyOfRejectsACompositeKey()
        {
            using var compositeContext = CompositeKeyContext.CreateSeeded(rows: 1);
            var entity = compositeContext.Set<CompositeKeyEntity>().First();

            Assert.Throws<System.InvalidOperationException>(() => compositeContext.KeyOf<CompositeKeyEntity, int>(entity));
        }
    }
}
