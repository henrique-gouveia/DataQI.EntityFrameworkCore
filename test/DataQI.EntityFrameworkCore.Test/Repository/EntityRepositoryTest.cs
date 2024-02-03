using System;
using System.Collections.Generic;
using System.Linq;

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFind(bool useAsyncMethod)
        {
            var customersList = InsertTestCustomersList();
            var customersEnumerator = customersList.GetEnumerator();

            while (customersEnumerator.MoveNext())
            {
                var customer = customersEnumerator.Current;
                var customerFullNameStartsWith = customer.FullName.Substring(0, 5);

                Func<ICriteria, ICriteria> criteriaBuilder = criteria =>
                    criteria.Add(Restrictions.StartingWith($"{nameof(Customer.FullName)}", customerFullNameStartsWith));

                var customersExpected = customersList
                    .Where(c => c.FullName.StartsWith(customerFullNameStartsWith))
                    .ToList();

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
            IEnumerable<Customer> customers = null;

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
        private bool disposedValue = false;

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