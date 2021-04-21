using System.Collections.Generic;
using DataQI.EntityFrameworkCore.Repository;

namespace DataQI.EntityFrameworkCore.Test.Repository.Products
{
    public interface IProductRepository : IEntityRepository<Product, int>
    {
        IEnumerable<Product> FindByEanLike(string ean);

        IEnumerable<Product> FindByIdOrEanOrReference(int id, string ean, string reference);

        IEnumerable<Product> FindByNameLikeAndStockGreaterThan(string name, decimal stock = 0);

        IEnumerable<Product> FindByDepartmentInAndNameLike(string[] departments, string name);

        IEnumerable<Product> FindByKeywordsLikeAndActive(string keywords, bool active = true);
    }
}