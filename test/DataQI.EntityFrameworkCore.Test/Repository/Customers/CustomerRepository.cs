using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

using Microsoft.EntityFrameworkCore;

using DataQI.EntityFrameworkCore.Repository.Support;

namespace DataQI.EntityFrameworkCore.Test.Repository.Customers
{
    public class CustomerRepository : EntityRepository<Customer, int>, ICustomerRepository
    {
        public CustomerRepository(DbContext context) : base(context)
        {

        }

        public IEnumerable<Customer> FindByFullName(string fullName)
        {
            var customers = context
                .Set<Customer>()
                .Where("(FullName == @0)", fullName)
                .AsNoTracking()
                .ToList();

            return customers;
        }

        public IEnumerable<Customer> FindByFullNameLikeAndActive(string fullName, bool active = true)
        {
            var customers = context
                .Set<Customer>()
                .Where("(FullName.StartsWith(@0) && Active == @1)", fullName, active)
                .AsNoTracking()
                .ToList();

            return customers;
        }

        public IEnumerable<Customer> FindByEmailLikeAndPhoneNotNull(string email)
        {
            var customers = context
                .Set<Customer>()
                .Where("(Email.EndsWith(@0) && Phone != null)", email)
                .AsNoTracking()
                .ToList();

            return customers;
        }

        public IEnumerable<Customer> FindByDateOfBirthBetween(DateTime startDate, DateTime endDate)
        {
            var customers = context
                .Set<Customer>()
                .Where("(DateOfBirth >= @0 && DateOfBirth <= @1)", startDate, endDate)
                .AsNoTracking()
                .ToList();

            return customers;
        }

        public IEnumerable<Customer> FindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan(DateTime dateRegister, DateTime dateOfBirth)
        {
            var customers = context
                .Set<Customer>()
                .Where("(DateRegister <= @0) || (DateOfBirth > @1)", dateRegister, dateOfBirth)
                .AsNoTracking()
                .ToList();

            return customers;
        }
    }
}