using System.Linq;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using DataQI.EntityFrameworkCore.Test.Repository.Customers;
using DataQI.EntityFrameworkCore.Test.Repository.Employees;
using DataQI.EntityFrameworkCore.Test.Repository.Products;

namespace DataQI.EntityFrameworkCore.Test
{
    public class TestContext : DbContext
    {
        protected TestContext(DbContextOptions options) : base(options)
        {
            TryCreateDabase();
        }

        public static TestContext NewInstance(string dbName = "dbtest.s3db")
        {
            var connection = new SqliteConnection($"Data Source={dbName}");
            var optionsBuilder = new DbContextOptionsBuilder().UseSqlite(connection);

            var context = new TestContext(optionsBuilder.Options);

            return context;
        }

        private bool TryCreateDabase()
        {
            try
            {
                return Database.EnsureCreated();
            }
            catch
            {
                return false;
            }
        }

        public void ClearCustomers() => Customers.RemoveRange(Customers.ToList());
        public void ClearDepartments() => Departments.RemoveRange(Departments.ToList());
        public void ClearEmployess() => Employees.RemoveRange(Employees.ToList());
        public void ClearProducts() => Products.RemoveRange(Products.ToList());

        public DbSet<Customer> Customers { get; private set; }
        public DbSet<Department> Departments { get; private set; }
        public DbSet<Employee> Employees { get; private set; }
        public DbSet<Product> Products { get; private set; }
    }
}