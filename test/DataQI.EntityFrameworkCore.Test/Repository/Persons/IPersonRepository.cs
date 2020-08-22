using System;
using System.Collections.Generic;
using DataQI.Commons.Repository;

namespace DataQI.EntityFrameworkCore.Test.Repository.Persons
{
    public interface IPersonRepository: ICrudRepository<Person, int>
    {
        IEnumerable<Person> FindByFullName(string fullName);

        IEnumerable<Person> FindByFullNameLikeAndActive(string name, bool active = true);

        IEnumerable<Person> FindByEmailLikeAndPhoneIsNotNull(string email);

        IEnumerable<Person> FindByDateOfBirthBetween(DateTime startDate, DateTime endDate);

        IEnumerable<Person> FindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan(DateTime dateRegister, DateTime dateOfBirth);
    }
}