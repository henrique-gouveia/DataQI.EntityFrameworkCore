using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

using Microsoft.EntityFrameworkCore;

using DataQI.EntityFrameworkCore.Repository.Support;

namespace DataQI.EntityFrameworkCore.Test.Repository.Persons
{
    public class PersonRepository : EntityRepository<Person, int>, IPersonRepository
    {
        public PersonRepository(DbContext context) : base(context)
        {
            
        }

        public IEnumerable<Person> FindByFullName(string fullName)
        {
            var persons = context
                .Set<Person>()
                .Where("(FullName == @0)", fullName)
                .AsNoTracking()
                .ToList();

            return persons;
        }

        public IEnumerable<Person> FindByFullNameLikeAndActive(string name, bool active = true)
        {
            var persons = context
                .Set<Person>()
                .Where("(FullName.StartsWith(@0) && Active == @1)", name, active)
                .AsNoTracking()
                .ToList();

            return persons;
        }

        public IEnumerable<Person> FindByEmailLikeAndPhoneIsNotNull(string email)
        {
            var persons = context
                .Set<Person>()
                .Where("(email.EndsWith(@0))", email)
                .AsNoTracking()
                .ToList();

            return persons;
        }

        public IEnumerable<Person> FindByDateOfBirthBetween(DateTime startDate, DateTime endDate)
        {
            var persons = context
                .Set<Person>()
                .Where("(DateOfBirth >= @0 and DateOfBirth <= @1)", startDate, endDate)
                .AsNoTracking()
                .ToList();

            return persons;
        }

        public IEnumerable<Person> FindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan(DateTime dateRegister, DateTime dateOfBirth)
        {
            var persons = context
                .Set<Person>()
                .Where("(DateRegister <= @0) or (DateOfBirth > @1)", dateRegister, dateOfBirth)
                .AsNoTracking()
                .ToList();

            return persons;
        }
    }
}