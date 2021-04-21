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
            // CustomerRepository = new CustomerRepository(Context);
            // EmployeeRepository = new EmployeeRepository(Context);
            // ProductRepository = new ProductRepository(Context);

            // 2. Provided
            var repositoryFactory = new EntityRepositoryFactory(CustomerContext);
            CustomerRepository = repositoryFactory.GetRepository<ICustomerRepository>();

            repositoryFactory = new EntityRepositoryFactory(EmployeeContext);
            EmployeeRepository = repositoryFactory.GetRepository<IEmployeeRepository>(new EmployeeRepository(EmployeeContext));

            repositoryFactory = new EntityRepositoryFactory(ProductContext);
            ProductRepository = repositoryFactory.GetRepository<IProductRepository>();
        }

        public TestContext CustomerContext { get; }
        public TestContext EmployeeContext { get; }
        public TestContext ProductContext { get; }

        public ICustomerRepository CustomerRepository { get; }
        public IEmployeeRepository EmployeeRepository { get; }
        public IProductRepository ProductRepository { get; }
    }
}

