using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using ExpectedObjects;
using Xunit;

using DataQI.Commons.Query;
using DataQI.Commons.Query.Support;

using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Repository.Customers;
using DataQI.EntityFrameworkCore.Repository;
using DataQI.EntityFrameworkCore.Repository.Support;

namespace DataQI.EntityFrameworkCore.Test.Repository
{
    public sealed class EntityRepositoryTest : IClassFixture<DbFixture>, IDisposable
    {
        private readonly TestContext customerContext;
        private readonly IEntityRepository<Customer, int> customerRepository;

        public EntityRepositoryTest(DbFixture fixture)
        {
            customerContext = fixture.CustomerContext;
            customerRepository = fixture.CustomerRepository;
        }

        [Fact]
        public void TestRejectsNullContext()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                new EntityRepository<Customer, int>(null));
            var baseException = exception.GetBaseException();

            Assert.IsType<ArgumentException>(baseException);
            Assert.Equal("DbContext must not be null", baseException.Message);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestInsertRejectsNullEntity(bool useAsyncMethod)
        {
            try
            {
                if (useAsyncMethod)
                    await customerRepository.InsertAsync(null);
                else
                    customerRepository.Insert(null);
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                Assert.IsType<ArgumentException>(baseException);
                Assert.Equal("Entity must not be null", baseException.Message);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestInsert(bool useAsyncMethod)
        {
            var countBefore = customerContext.Customers.CountAsync().Result;
            var countExpected = ++countBefore;

            var customerExpected = CustomerBuilder.NewInstance().Build();
            if (useAsyncMethod)
                customerRepository.InsertAsync(customerExpected).Wait();
            else
                customerRepository.Insert(customerExpected);

            customerContext.SaveChanges();

            Assert.True(customerExpected.Id > 0);
            Assert.Equal(countExpected, customerContext.Customers.CountAsync().Result);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestSaveRejectsNullEntity(bool useAsyncMethod)
        {
            try
            {
                if (useAsyncMethod)
                    await customerRepository.SaveAsync(null);
                else
                    customerRepository.Save(null);
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                Assert.IsType<ArgumentException>(baseException);
                Assert.Equal("Entity must not be null", baseException.Message);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestSave(bool useAsyncMethod)
        {
            var countBefore = customerContext.Set<Customer>().Count();
            var countExpected = ++countBefore;

            var customerInserted = CustomerBuilder.NewInstance().Build();
            SaveCustomer(customerInserted, useAsyncMethod);

            var customerUpdated = CustomerBuilder.NewInstance().SetId(customerInserted.Id).Build();
            SaveCustomer(customerUpdated, useAsyncMethod);

            var customerFinded = customerContext.Find<Customer>(customerUpdated.Id);

            customerUpdated.ToExpectedObject().ShouldMatch(customerFinded);
            Assert.Equal(countExpected, customerContext.Set<Customer>().Count());
        }

        private void SaveCustomer(Customer customer, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                customerRepository.SaveAsync(customer).Wait();
            else
                customerRepository.Save(customer);

            customerContext.SaveChanges();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestExistsReturnsTrue(bool useAsyncMethod)
        {
            var customersExpected = InsertTestCustomers();
            while (customersExpected.MoveNext())
            {
                var customer = customersExpected.Current;
                bool customerExists = ExistsCustomer(customer, useAsyncMethod);

                Assert.True(customerExists);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestExistsReturnsFalse(bool useAsyncMethod)
        {
            InsertTestCustomers();
            var customerExists = ExistsCustomer(new Customer(), useAsyncMethod);
            Assert.False(customerExists);
        }
        
        [Fact]
        public void TestFind()
        {
            var customersExpected = InsertTestCustomersList();
            var customers = customerRepository.Find().ToList();
            customersExpected.ToExpectedObject().ShouldMatch(customers);
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFindRejectsNullPredicate(bool useAsyncMethod)
        {
            try
            {
                Expression<Func<Customer, bool>> predicate = null;
                if (useAsyncMethod)
                    await customerRepository.FindAsync(predicate);
                else
                    customerRepository.Find(predicate);
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                Assert.IsType<ArgumentException>(baseException);
                Assert.Equal("Predicate must not be null", baseException.Message);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindByPredicate(bool useAsyncMethod)
        {
            var customersList = InsertTestCustomersList();
            using var customersEnumerator = customersList.GetEnumerator();
            while (customersEnumerator.MoveNext())
            {
                var customer = customersEnumerator.Current;
                var customersExpected = customersList
                    .Where(c => c.Document == customer?.Document);

                Expression<Func<Customer, bool>> predicate = c =>
                    c.Document == customer.Document;

                IEnumerable<Customer> customers;
                if (useAsyncMethod)
                    customers = customerRepository.FindAsync(predicate).Result;
                else
                    customers = customerRepository.Find(predicate);
                
                customersExpected.ToExpectedObject().ShouldMatch(customers);
            }
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFindRejectsNullQueryBuilder(bool useAsyncMethod)
        {
            try
            {
                Func<IQueryable<Customer>, IQueryable<Customer>> query = null;
                if (useAsyncMethod)
                    await customerRepository.FindAsync(query);
                else
                    customerRepository.Find(query);
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                Assert.IsType<ArgumentException>(baseException);
                Assert.Equal("QueryBuilder must not be null", baseException.Message);
            }
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindByQueryBuilder(bool useAsyncMethod)
        {
            var customersList = InsertTestCustomersList();
            using var customersEnumerator = customersList.GetEnumerator();
        
            while (customersEnumerator.MoveNext())
            {
                var customer = customersEnumerator.Current;
                var customerEmailDomain = customer?.Email.Split('@')[1] ?? "";
                var customerActive = customer?.Active;
                var customersExpected = customersList
                    .Where(c => 
                        c.Email.EndsWith(customerEmailDomain)
                        && c.Active == customerActive)
                    .ToList();
        
                Func<IQueryable<Customer>, IQueryable<Customer>> queryBuilder = query =>
                    query.Where(c => 
                        c.Email.EndsWith(customerEmailDomain)
                        && c.Active == customerActive);
                
                IEnumerable<Customer> customers;
                if (useAsyncMethod)
                    customers = customerRepository.FindAsync(queryBuilder).Result;
                else
                    customers = customerRepository.Find(queryBuilder);
                
                customersExpected.ToExpectedObject().ShouldMatch(customers);
            }
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFindRejectsNullCriteria(bool useAsyncMethod)
        {
            try
            {
                Func<ICriteria, ICriteria> criteriaBuilder = null;
                if (useAsyncMethod)
                    await customerRepository.FindAsync(criteriaBuilder);
                else
                    customerRepository.Find(criteriaBuilder);
            }
            catch (Exception e)
            {
                var baseException = e.GetBaseException();
                Assert.IsType<ArgumentException>(baseException);
                Assert.Equal("CriteriaBuilder must not be null", baseException.Message);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindByCriteria(bool useAsyncMethod)
        {
            var customersList = InsertTestCustomersList();
            using var customersEnumerator = customersList.GetEnumerator();
            while (customersEnumerator.MoveNext())
            {
                var customer = customersEnumerator.Current;
                var customerFullNameStartsWith = customer?.FullName.Substring(0, 5);
                var customersExpected = customersList
                    .Where(c => c.FullName.StartsWith(customerFullNameStartsWith ?? ""))
                    .ToList();

                Func<ICriteria, ICriteria> criteriaBuilder = criteria =>
                    criteria.Add(Restrictions.StartingWith($"{nameof(Customer.FullName)}", customerFullNameStartsWith));
                
                IEnumerable<Customer> customers;
                if (useAsyncMethod)
                    customers = customerRepository.FindAsync(criteriaBuilder).Result;
                else
                    customers = customerRepository.Find(criteriaBuilder);

                customersExpected.ToExpectedObject().ShouldMatch(customers);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindAll(bool useAsyncMethod)
        {
            var customersExpected = InsertTestCustomersList();
            IEnumerable<Customer> customers;

            if (useAsyncMethod)
                customers = customerRepository.FindAllAsync().Result;
            else
                customers = customerRepository.FindAll();

            customersExpected.ToExpectedObject().ShouldMatch(customers);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindOneReturnsEntity(bool useAsyncMethod)
        {
            var customersExpected = InsertTestCustomers();
            while (customersExpected.MoveNext())
            {
                var customerExpected = customersExpected.Current;
                Customer customer = FindOneCustomer(customerExpected, useAsyncMethod);

                customerExpected.ToExpectedObject().ShouldMatch(customer);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindOneReturnsNull(bool useAsyncMethod)
        {
            InsertTestCustomers();
            var customer = FindOneCustomer(new Customer(), useAsyncMethod);

            Assert.Null(customer);
        }

        private Customer FindOneCustomer(Customer customer, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                return customerRepository.FindOneAsync(customer.Id).Result;
            else
                return customerRepository.FindOne(customer.Id);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDelete(bool useAsyncMethod)
        {
            var customers = InsertTestCustomers();
            while (customers.MoveNext())
            {
                var customer = customers.Current;
                if (useAsyncMethod)
                    customerRepository.DeleteAsync(customer.Id).Wait();
                else
                    customerRepository.Delete(customer.Id);

                customerContext.SaveChanges();

                Assert.False(ExistsCustomer(customer, useAsyncMethod));
                Assert.Null(FindOneCustomer(customer, useAsyncMethod));
            }
        }

        private bool ExistsCustomer(Customer customer, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                return customerRepository.ExistsAsync(customer.Id).Result;
            else
                return customerRepository.Exists(customer.Id);
        }
        
        private IEnumerator<Customer> InsertTestCustomers()
        {
            var customers = InsertTestCustomersList();
            return customers.GetEnumerator();
        }

        private IList<Customer> InsertTestCustomersList()
        {
            var customers = new List<Customer>()
            {
                CustomerBuilder.NewInstance().Build(),
                CustomerBuilder.NewInstance().Build(),
                CustomerBuilder.NewInstance().Build(),
                CustomerBuilder.NewInstance().Build(),
                CustomerBuilder.NewInstance().Build(),
            };

            customers.ForEach(p =>
            {
                customerRepository.Save(p);
                customerContext.SaveChanges();

                Assert.True(customerRepository.Exists(p.Id));
            });

            return customers;
        }

        #region IDisposable Support
        private bool disposedValue;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                customerContext.ClearCustomers();
                customerContext.SaveChanges();
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