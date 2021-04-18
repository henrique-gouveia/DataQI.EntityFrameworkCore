using System;
using System.Collections.Generic;

using DataQI.EntityFrameworkCore.Repository;

namespace DataQI.EntityFrameworkCore.Test.Repository.Customers
{
    public interface ICustomerRepository : IEntityRepository<Customer, int>
    {
        IEnumerable<Customer> FindByFullName(string fullName);

        IEnumerable<Customer> FindByFullNameLikeAndActive(string name, bool active = true);

        IEnumerable<Customer> FindByEmailLikeAndPhoneNotNull(string email);

        IEnumerable<Customer> FindByDateOfBirthBetween(DateTime startDate, DateTime endDate);

        IEnumerable<Customer> FindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan(DateTime dateRegister, DateTime dateOfBirth);
    }
}