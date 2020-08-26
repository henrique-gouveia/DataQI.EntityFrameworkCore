using System;
using System.Collections.Generic;
using DataQI.EntityFrameworkCore.Repository;

namespace DataQI.EntityFrameworkCore.Test.Repository.Persons
{
    public interface IPersonRepository: IEntityRepository<Person, int>
    {
        IEnumerable<Person> FindByFullName(string fullName);

        IEnumerable<Person> FindByFullNameLikeAndActive(string name, bool active = true);

        IEnumerable<Person> FindByEmailLikeAndPhoneNotNull(string email);

        IEnumerable<Person> FindByDateOfBirthBetween(DateTime startDate, DateTime endDate);

        IEnumerable<Person> FindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan(DateTime dateRegister, DateTime dateOfBirth);
    }
}