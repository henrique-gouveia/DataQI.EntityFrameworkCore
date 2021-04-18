using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using ExpectedObjects;
using Xunit;

using DataQI.EntityFrameworkCore.Test.Fixtures;
using DataQI.EntityFrameworkCore.Test.Repository.Customers;

namespace DataQI.EntityFrameworkCore.Test.Repository
{
    public class EntityRepositoryTest : IClassFixture<DbFixture>, IDisposable
    {
        private readonly TestContext context;
        private readonly ICustomerRepository customerRepository;

        public EntityRepositoryTest(DbFixture fixture)
        {
            context = fixture.Context;
            customerRepository = fixture.CustomerRepository;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestInsert(bool useAsyncMethod)
        {
            var countBefore = context.Customers.CountAsync().Result;
            var countExpected = ++countBefore;

            var customerExpected = CustomerBuilder.NewInstance().Build();
            if (useAsyncMethod)
                customerRepository.InsertAsync(customerExpected).Wait();
            else
                customerRepository.Insert(customerExpected);

            context.SaveChanges();

            Assert.True(customerExpected.Id > 0);
            Assert.Equal(countExpected, context.Customers.CountAsync().Result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestSave(bool useAsyncMethod)
        {
            var countBefore = context.Set<Customer>().Count();
            var countExpected = ++countBefore;

            var customerInserted = CustomerBuilder.NewInstance().Build();
            SaveCustomer(customerInserted, useAsyncMethod);

            var customerUpdated = CustomerBuilder.NewInstance().SetId(customerInserted.Id).Build();
            SaveCustomer(customerUpdated, useAsyncMethod);

            var customerFinded = context.Find<Customer>(customerUpdated.Id);

            customerUpdated.ToExpectedObject().ShouldMatch(customerFinded);
            Assert.Equal(countExpected, context.Set<Customer>().Count());
        }

        private void SaveCustomer(Customer customer, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                customerRepository.SaveAsync(customer).Wait();
            else
                customerRepository.Save(customer);

            context.SaveChanges();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestExists(bool useAsyncMethod)
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
        public void TestExistsEntityNotFoundReturnsFalse(bool useAsyncMethod)
        {
            InsertTestCustomers();
            var customerExists = ExistsCustomer(new Customer(), useAsyncMethod);
            Assert.False(customerExists);
        }

        private bool ExistsCustomer(Customer customer, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                return customerRepository.ExistsAsync(customer.Id).Result;
            else
                return customerRepository.Exists(customer.Id);
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
        public void TestFindOne(bool useAsyncMethod)
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
        public void TestFindOneEntityNotFoundReturnsNull(bool useAsyncMethod)
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

                context.SaveChanges();

                Assert.False(ExistsCustomer(customer, useAsyncMethod));
                Assert.Null(FindOneCustomer(customer, useAsyncMethod));
            }
        }

        [Fact]
        public void TestFindByFullName()
        {
            var customersExpected = InsertTestCustomers();

            while (customersExpected.MoveNext())
            {
                var customerExpected = customersExpected.Current;
                var customers = customerRepository.FindByFullName(customerExpected.FullName);

                customerExpected.ToExpectedObject().ShouldMatch(customers.FirstOrDefault());
            }
        }


        [Fact]
        public void TestFindByFullNameLikeAndActive()
        {
            var customersExpected = InsertTestCustomers();

            while (customersExpected.MoveNext())
            {
                var customerExpected = customersExpected.Current;
                var customers = customerRepository.FindByFullNameLikeAndActive(customerExpected.FullName, customerExpected.Active);

                customerExpected.ToExpectedObject().ShouldMatch(customers.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByEmailLikeAndPhoneNotNull()
        {
            var customersExpected = InsertTestCustomers();

            while (customersExpected.MoveNext())
            {
                var customerExpected = customersExpected.Current;
                var customers = customerRepository.FindByEmailLikeAndPhoneNotNull(customerExpected.Email);

                customerExpected.ToExpectedObject().ShouldMatch(customers.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByDateOfBirthBetween()
        {
            var customersExpected = InsertTestCustomers();

            while (customersExpected.MoveNext())
            {
                var customerExpected = customersExpected.Current;
                var customers = customerRepository.FindByDateOfBirthBetween(customerExpected.DateOfBirth, customerExpected.DateOfBirth);

                customerExpected.ToExpectedObject().ShouldMatch(customers.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan()
        {
            var customersList = InsertTestCustomersList();

            var dateRegisterMax = customersList.Select(p => p.DateRegister).Max();
            var dateOfBirthMin = customersList.Select(p => p.DateOfBirth).Min();

            var customersExpected = customersList.Where(p => p.DateRegister < dateRegisterMax || p.DateOfBirth >= dateOfBirthMin);

            var customers = customerRepository.FindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan(dateRegisterMax, dateOfBirthMin);
            customersExpected.ToExpectedObject().ShouldMatch(customers);
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
                context.SaveChanges();

                Assert.True(customerRepository.Exists(p.Id));
            });

            return customers;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                context.ClearCustomers();
                context.SaveChanges();
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