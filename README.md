# DataQI EntityFrameworkCore

Data Query Interface Provider for [EntityFrameworkCore](https://github.com/dotnet/efcore) written in C# and built around essential features of the .NET Standard that use infraestructure provided by [DataQI.Commons](https://github.com/henrique-gouveia/DataQI.Commons) and it turns your Data Repositories a live interface. Its purpose is to facilitate the construction of data access layers and makes possible the definition repository interfaces, providing behaviors for standard operations as well to defines customized queries through method signatures.

[![NuGet](https://img.shields.io/nuget/v/DataQI.EntityFrameworkCore.svg)](https://www.nuget.org/packages/DataQI.EntityFrameworkCore/)
[![Build status](https://ci.appveyor.com/api/projects/status/rl2ja69994rt3ei6?svg=true)](https://ci.appveyor.com/project/henrique-gouveia/dataqi-entityframeworkcore)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.EntityFrameworkCore/branch/master/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.EntityFrameworkCore)

## Getting Started

### Installing

This library can add in to the project by way:

    dotnet add package DataQI.EntityFrameworkCore

See [Nuget](https://www.nuget.org/packages/DataQI.EntityFrameworkCore) for other options.

### Defining a Repository

A Repository Interface should extends the interface `IEntityRepository<TEntity>` localized in the namespace `DataQI.EntityFrameworkCore.Repository`, where the `TEntity` is a _Plain Old CSharp Object (POCO)_ and `TId` is its key type.

```csharp
[Table("Person")]
public class Person
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("person_id")]
    public int Id { get; set; }
    [Column("first_name")]
    public string FirstName { get; set; }
    [Column("last_name")]
    public string LastName { get; set; }
    [Column("birth_date")]
    public DateTime BirthDate { get; set; }
    [Column("register_date")]
    public DateTime RegisterDate { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public boolean Active { get; set; }
}

public interface IPersonRepository : IEntityRepository<Person, int>
{

}
```

### Instancing a Repository

Should to use a instance of the `EntityRepositoryFactory` class to instantiate a Repository, localizated in the namespace `DataQI.EntityFrameworkCore.Repository.Support`, that requires a `DbContext` to make its calls:

```csharp
DbContext dbContext = CreateDbContext();
var repositoryFactory = new EntityRepositoryFactory(dbContext);

personRepository = repositoryFactory.GetRepository<IPersonRepository>();
```

### Using Default Methods

A Repository Interface that extends `IEntityRepository<TEntity>` inherit its standard operations:

```csharp
personRepository.Insert(person);
await personRepository.InsertAsync(person);

personRepository.Save(person);
await personRepository.SaveAsync(person);

personRepository.Delete(person);
await personRepository.DeleteAsync(person);

var exists = personRepository.Exists(person);
exists = await personRepository.ExistsAsync(person);

var allPersons = personRepository.FindAll();
allPersons = await personRepository.FindAllAsync();

var onePerson = personRepository.FindOne(1);
onePerson = await personRepository.FindOneAsync(1);
```

### Using Criteria Definitions

Customized Queries can be specified by a simple Criteria Query API where are the main artifacts is localized in the namespace `DataQI.Common.Query` and `DataQI.Common.Query.Support`.

```csharp
var personsByCriteria = personRepository.Find(criteria =>
    criteria
        .Add(Restrictions.Like("FirstName", "Name%"))
        .Add(Restrictions
            .Disjuction()
            .Add(Restrictions.Between("BirthDate", new DateTime(2015, 1, 1), new DateTime(2020, 1, 1)))
            .Add(Restrictions.Equal("Active", true)))
    );

var personsByCriteriaAsync = await personRepository.FindAsync(criteria =>
    criteria
        .Add(Restrictions.Like("LastName", "%Name%"))
        .Add(Restrictions
            .Disjuction()
            .Add(Restrictions.Between("BirthDate", new DateTime(2015, 1, 1), new DateTime(2020, 1, 1)))
            .Add(Restrictions.GreaterThan("RegisterDate", new DateTime(2019, 1, 1))))
    );
```

### Using Query Methods

Customized Queries can be defined through method signatures with the following conventions:

- The method name can be initiated with the prefix `FindBy`.
- Next step, should be indicated the field that will be want to apply a operator.
- After the field name, should be indicated the operator (column `Operador` from the table below). The `Equal` is assumed how default operator if nothing it's indicate.
- Finaly, each sentence composition can be combined with anothers through of the `Conjunctions` _AND_ and `Disjunction` _OR_.

#### Supported keywords inside method names

| **Keyword** | **Sample** | **Fragment**
|-------------|------------|-------------
| **Equal** | FindByName, FindByName**Equal** | where Name **=** @0
| **NotEqual** | FindByName**Not**, FindByName**NotEqual** | where Name **!=** @0
| **Between** | FindByAge**Between** | where Age **>=** @0 **&&** Age **<=** @1
| **NotBetween** | FindByAge**NotBetween** | where !(Age **>=** @0 **&&** Age **<=** @1)
| **GreaterThan** | FindByDateOfBirth**GreaterThan** | where DateOfBirth **>** @0
| **GreaterThanEqual** | FindByDateOfBirth**GreaterThanEqual** | where DateOfBirth **>=** @0
| **LessThan** | FindByDateOfBirth**LessThan** | where DateOfBirth **<** @0
| **LessThanEqual** | FindByDateOfBirth**LessThanEqual** | where DateOfBirth **<=** @0
| **In** | FindByAddressType**In** | where **@0.Contains**(AddressType)
| **NotIn** | FindByAddressType**NotIn** | where !**@0.Contains**(AddressType)
| **Null** | FindByEmail**Null** | where Email **== null**
| **NotNull** | FindByEmail**NotNull** | where Email **!= null**
| **StartingWith** | FindByName**StartingWith** | where Name.**StartsWith(@0)**
| **NotStartingWith** | FindByName**NotStartingWith** | where !Name.**StartsWith(@0)**
| **EndingWith** | FindByName**EndingWith** | where Name.**EndsWith(@0)**
| **NotEndingWith** | FindByName**NotEndingWith** | where !Name.**EndsWith(@0)**
| **Containing** | FindByName**Containing** | where Name.**Contains(@0)**
| **NotContaining** | FindByName**NotContaining** | where !Name.**Contains(@0)**
| **Like** | FindByName**Like** | where Name.**Contains(@0)**
| **NotLike** | FindByName**NotLike** | where where !Name.**Contains(@0)**
| **And** | FindByName**And**Email | where (Name = @0 **&&** Email = @1)
| **Or** | FindByName**Or**Email | where (Name = @0 **\|\|** Email = @1)

#### Sample

```csharp
public interface IPersonRepository : IEntityRepository<Person, int>
{
    IEnumerable<Person> FindByLastName(string name);
    IEnumerable<Person> FindByDateOfBirthBetween(DateTime startDate, DateTime endDate);
    IEnumerable<Person> FindByFirstNameLikeAndActive(string name, bool active = true);
    IEnumerable<Person> FindByEmailLikeOrPhoneNotNull(string email);
    IEnumerable<Person> FindByFirstNameAndLastNameOrDateOfBirthGreaterThan(string firstName, string lastName, DateTime registerDate);
}

DbContext dbContext = CreateDbContext();
var repositoryFactory = new EntityRepositoryFactory(dbContext);

var personRepository = factory.GetRepository<IPersonRepository>();

var persons = personRepository.FindByLastName("A Last Name");
persons = personRepository.FindByDateOfBirthBetween(new DateTime(2015, 1, 1), new DateTime(2020, 1, 1));
persons = personRepository.FindByFirstNameLikeAndActive(string name, bool active = true);
persons = personRepository.FindByEmailLikeOrPhoneNotNull(string email);
persons = personRepository.FindFindByFirstNameAndLastNameOrDateOfBirthGreaterThan("A First Name", "A Last Name", new DateTime(2019, 1, 1));
```

## News

**v1.0.0 - 2020/09**

* Provided initial core base

## Limitations and caveats

DataQI does attempt to solve some problems. It is in the experimental phase.

## License

DataQI EntityFrameworkCore is released under the [MIT License](https://opensource.org/licenses/MIT).
