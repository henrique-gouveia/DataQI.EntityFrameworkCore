using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

using Microsoft.EntityFrameworkCore;

using DataQI.EntityFrameworkCore.Repository.Support;

namespace DataQI.EntityFrameworkCore.Test.Repository.Products
{
    public class ProductRepository : EntityRepository<Product, int>
    {
        public ProductRepository(DbContext context) : base(context)
        {

        }

        public IEnumerable<Product> FindByEanLike(string ean)
        {
            var products = context
                .Set<Product>()
                .Where($"({nameof(Product.Ean)}.StartsWith(@0))", ean)
                .AsNoTracking()
                .ToList();

            return products;
        }

        public IEnumerable<Product> FindByIdOrEanOrReference(int id, string ean, string reference)
        {
            var products = context
                .Set<Product>()
                .Where($"({nameof(Product.Id)} == @0 || {nameof(Product.Ean)} == @1 || {nameof(Product.Reference)}) == @2", id, ean, reference)
                .AsNoTracking()
                .ToList();

            return products;
        }

        public IEnumerable<Product> FindByNameLikeAndStockGreaterThan(string name, decimal stock = 0)
        {
            var products = context
                .Set<Product>()
                .Where($"({nameof(Product.Name)}.StartsWith(@0) && {nameof(Product.Stock)} > @1)", name, stock)
                .AsNoTracking()
                .ToList();

            return products;
        }

        public IEnumerable<Product> FindByDepartmentInAndNameStartingWith(string[] departments, string name)
        {
            var products = context
                .Set<Product>()
                .Where($"(@0.Contains({nameof(Product.Department)}) && {nameof(Product.Name)}.StartsWith(@1))", departments, name)
                .AsNoTracking()
                .ToList();

            return products;
        }

        public IEnumerable<Product> FindByKeywordsLikeAndActive(string keywords, bool active = true)
        {
            var products = context
                 .Set<Product>()
                 .Where($"({nameof(Product.Keywords)}.Contains(@0) && {nameof(Product.Active)} == @1)", keywords, active)
                 .AsNoTracking()
                 .ToList();

            return products;
        }
    }
}