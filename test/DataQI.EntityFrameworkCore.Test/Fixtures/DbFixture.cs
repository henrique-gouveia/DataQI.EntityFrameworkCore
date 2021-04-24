using DataQI.EntityFrameworkCore.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;

using DataQI.EntityFrameworkCore.Test.Repository.Customers;
using DataQI.EntityFrameworkCore.Test.Repository.Employees;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test.Fixtures
{
    public class DbFixture
    {
        public DbFixture()
        {
            CustomerContext = TestContext.NewInstance();
            EmployeeContext = TestContext.NewInstance();
            ProductContext = TestContext.NewInstance();

            // 1. Default
            // CustomerRepository = new EntityRepository<Customer, int>(Context);
            // EmployeeRepository = new EmployeeRepository(Context);
            // ProductRepository = new ProductRepository(Context);

            // 2. Provided
            var repositoryFactory = new EntityRepositoryFactory();

            CustomerRepository = repositoryFactory.GetRepository<IEntityRepository<Customer, int>>(CustomerContext);
            EmployeeRepository = repositoryFactory.GetRepository<IEmployeeRepository>(() => new EmployeeRepository(EmployeeContext));
            ProductRepository = repositoryFactory.GetRepository<IProductRepository>(ProductContext);
        }

        public TestContext CustomerContext { get; }
        public TestContext EmployeeContext { get; }
        public TestContext ProductContext { get; }

        public IEntityRepository<Customer, int> CustomerRepository { get; }
        public IEmployeeRepository EmployeeRepository { get; }
        public IProductRepository ProductRepository { get; }
    }
}

