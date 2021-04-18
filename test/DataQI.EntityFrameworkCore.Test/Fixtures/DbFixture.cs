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
            Context = TestContext.NewInstance();

            // 1. Default
            // CustomerRepository = new CustomerRepository(Context);
            // EmployeeRepository = new EmployeeRepository(Context);
            // ProductRepository = new ProductRepository(Context);

            // 2. Provided
            var repositoryFactory = new EntityRepositoryFactory(Context);
            CustomerRepository = repositoryFactory.GetRepository<ICustomerRepository>();
            EmployeeRepository = repositoryFactory.GetRepository<IEmployeeRepository>(new EmployeeRepository(Context));
            ProductRepository = repositoryFactory.GetRepository<IProductRepository>();
        }

        public TestContext Context { get; }

        public ICustomerRepository CustomerRepository { get; }
        public IEmployeeRepository EmployeeRepository { get; }
        public IProductRepository ProductRepository { get; }
    }
}

