using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using ExpectedObjects;
using Xunit;

using DataQI.EntityFrameworkCore.Test.Fixtures;

namespace DataQI.EntityFrameworkCore.Test.Repository.Persons
{
    public class PersonRepositoryTest : IClassFixture<DbFixture>, IDisposable
    {
        private readonly TestContext context;
        private readonly IPersonRepository personRepository;

        public PersonRepositoryTest(DbFixture fixture)
        {
            context = fixture.Context;
            personRepository = fixture.PersonRepository;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestInsert(bool useAsyncMethod)
        {
            var countBefore = context.Persons.CountAsync().Result;
            var countExpected = ++countBefore;

            var personExpected = PersonBuilder.NewInstance().Build();
            if (useAsyncMethod)
                personRepository.InsertAsync(personExpected).Wait();
            else
                personRepository.Insert(personExpected);

            context.SaveChanges();

            Assert.True(personExpected.Id > 0);
            Assert.Equal(countExpected, context.Persons.CountAsync().Result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestSave(bool useAsyncMethod)
        {
            var countBefore = context.Set<Person>().Count();
            var countExpected = ++countBefore;

            var personInserted = PersonBuilder.NewInstance().Build();
            SavePerson(personInserted, useAsyncMethod);

            var personUpdated = PersonBuilder.NewInstance().SetId(personInserted.Id).Build();
            SavePerson(personUpdated, useAsyncMethod);

            var personFinded = context.Find<Person>(personUpdated.Id);
            
            personUpdated.ToExpectedObject().ShouldMatch(personFinded);
            Assert.Equal(countExpected, context.Set<Person>().Count());
        }

        private void SavePerson(Person person, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                personRepository.SaveAsync(person).Wait();
            else
                personRepository.Save(person);

            context.SaveChanges();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestExists(bool useAsyncMethod)
        {
            var personsExpected = InsertTestPersons();

            while (personsExpected.MoveNext())
            {
                var person = personsExpected.Current;
                bool personExists = ExistsPerson(person, useAsyncMethod);

                Assert.True(personExists);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestExistsEntityNotFoundReturnsFalse(bool useAsyncMethod)
        {
            InsertTestPersons();
            var personExists = ExistsPerson(new Person(), useAsyncMethod);
            Assert.False(personExists);
        }

        private bool ExistsPerson(Person person, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                return personRepository.ExistsAsync(person.Id).Result;
            else
                return personRepository.Exists(person.Id);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindAll(bool useAsyncMethod)
        {
            var personsExpected = InsertTestPersonsList();
            IEnumerable<Person> persons = null;

            if (useAsyncMethod)
                persons = personRepository.FindAllAsync().Result;
            else
                persons = personRepository.FindAll();

            personsExpected.ToExpectedObject().ShouldMatch(persons);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindOne(bool useAsyncMethod)
        {
            var personsExpected = InsertTestPersons();

            while (personsExpected.MoveNext())
            {
                var personExpected = personsExpected.Current;
                Person person = FindOnePerson(personExpected, useAsyncMethod);

                personExpected.ToExpectedObject().ShouldMatch(person);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestFindOneEntityNotFoundReturnsNull(bool useAsyncMethod)
        {
            InsertTestPersons();
            var person = FindOnePerson(new Person(), useAsyncMethod);

            Assert.Null(person);
        }

        private Person FindOnePerson(Person person, bool useAsyncMethod)
        {
            if (useAsyncMethod)
                return personRepository.FindOneAsync(person.Id).Result;
            else
                return personRepository.FindOne(person.Id);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TestDelete(bool useAsyncMethod)
        {
            var persons = InsertTestPersons();

            while (persons.MoveNext())
            {
                var person = persons.Current;
                if (useAsyncMethod)
                    personRepository.DeleteAsync(person.Id).Wait();
                else
                    personRepository.Delete(person.Id);

                context.SaveChanges();

                Assert.False(ExistsPerson(person, useAsyncMethod));
                Assert.Null(FindOnePerson(person, useAsyncMethod));
            }
        }

        [Fact]
        public void TestFindByFullName()
        {
            var personsExpected = InsertTestPersons();

            while (personsExpected.MoveNext())
            {
                var personExpected = personsExpected.Current;
                var persons = personRepository.FindByFullName(personExpected.FullName);

                personExpected.ToExpectedObject().ShouldMatch(persons.FirstOrDefault());
            }
        }


        [Fact]
        public void TestFindByFullNameLikeAndActive()
        {
            var personsExpected = InsertTestPersons();

            while(personsExpected.MoveNext())
            {
                var personExpected = personsExpected.Current;
                var persons = personRepository.FindByFullNameLikeAndActive(personExpected.FullName, personExpected.Active);

                personExpected.ToExpectedObject().ShouldMatch(persons.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByEmailLikeAndPhoneIsNotNull()
        {
            var personsExpected = InsertTestPersons();

            while(personsExpected.MoveNext())
            {
                var personExpected = personsExpected.Current;
                var persons = personRepository.FindByEmailLikeAndPhoneIsNotNull(personExpected.Email);

                personExpected.ToExpectedObject().ShouldMatch(persons.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByDateOfBirthBetween()
        {
            var personsExpected = InsertTestPersons();

            while(personsExpected.MoveNext())
            {
                var personExpected = personsExpected.Current;
                var persons = personRepository.FindByDateOfBirthBetween(personExpected.DateOfBirth, personExpected.DateOfBirth);

                personExpected.ToExpectedObject().ShouldMatch(persons.FirstOrDefault());
            }
        }

        [Fact]
        public void TestFindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan()
        {
            var personsList = InsertTestPersonsList();

            var dateRegisterMax = personsList.Select(p => p.DateRegister).Max();
            var dateOfBirthMin = personsList.Select(p => p.DateOfBirth).Min();

            var personsExpected = personsList.Where(p => p.DateRegister < dateRegisterMax || p.DateOfBirth >= dateOfBirthMin);

            var persons = personRepository.FindByDateRegisterLessThanEqualOrDateOfBirthGreaterThan(dateRegisterMax, dateOfBirthMin);
            personsExpected.ToExpectedObject().ShouldMatch(persons);
        }

        private IEnumerator<Person> InsertTestPersons()
        {
            var persons = InsertTestPersonsList();
            return persons.GetEnumerator();
        }

        private IList<Person>  InsertTestPersonsList()
        {
            var persons = new List<Person>()
            {
                PersonBuilder.NewInstance().Build(),
                PersonBuilder.NewInstance().Build(),
                PersonBuilder.NewInstance().Build(),
                PersonBuilder.NewInstance().Build(),
                PersonBuilder.NewInstance().Build(),
            };

            persons.ForEach(p =>
            {
                personRepository.Save(p);
                context.SaveChanges();

                Assert.True(personRepository.Exists(p.Id));
            });

            return persons;
        }              

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                context.ClearPersons();
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