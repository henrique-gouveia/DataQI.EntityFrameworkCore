# DataQI EntityFrameworkCore

Data Query Interface Provider for [EntityFrameworkCore](https://github.com/dotnet/efcore) written in C# and built around essential features of the .NET Standard that use infraestructure provided by [DataQI.Commons](https://github.com/henrique-gouveia/DataQI.Commons) and it turns your Data Repositories a live interface. Its purpose is to facilitate the construction of data access layers and makes possible the definition repository interfaces, providing behaviors for standard operations as well to defines customized queries through method signatures.

[![Build](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/actions/workflows/dotnet.yml/badge.svg)](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.EntityFrameworkCore/branch/main/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.EntityFrameworkCore)
[![NuGet](https://img.shields.io/nuget/v/DataQI.EntityFrameworkCore.svg)](https://www.nuget.org/packages/DataQI.EntityFrameworkCore/)
[![License](https://img.shields.io/github/license/henrique-gouveia/DataQI.EntityFrameworkCore.svg)](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/blob/main/LICENSE.txt)

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
var repositoryFactory = new EntityRepositoryFactory();

personRepository = repositoryFactory.GetRepository<IPersonRepository>(dbContext);
```

### Configuring by using Microsoft's Dependency Injection

After you have installed this lib, if you're using some type of .NET Web Application, you can do the following:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

Don't forget to configure your `DbContext`:

```csharp
// Configure DbContext like you prefer...
builder.Services.AddDbContext<DbContext>(...);
```

If you don't need a specific repository interface, you can just register a service by doing the following:

```csharp
// It'll add IEntityRepository<Entity, int> repository service...
builder.Services.AddDefaultEntityRepository<Person, int, DbContext>();
```

If you need a specific repository interface, you can register it by doing the following:

```csharp
// It'll add IPersonRepository repository service...
builder.Services.AddEntityRepository<IPersonRepository, DbContext>();
```

Now, if you need to add custom support using a repository implementation, you can register it by doing the following:

```csharp
// It'll add IPersonRepository repository service supported by SomeCustomRepository ...
builder.Services.AddEntityRepository<IPersonRepository, SomeCustomRepository, DbContext>();
```

Take a look at the [Samples](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/tree/main/samples) to see how this works.

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
| **GreaterThan** | FindByBirthDate**GreaterThan** | where BirthDate **>** @0
| **GreaterThanEqual** | FindByBirthDate**GreaterThanEqual** | where BirthDate **>=** @0
| **LessThan** | FindByBirthDate**LessThan** | where BirthDate **<** @0
| **LessThanEqual** | FindByBirthDate**LessThanEqual** | where BirthDate **<=** @0
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
| **NotLike** | FindByName**NotLike** | where !Name.**Contains(@0)**
| **And** | FindByName**And**Email | where (Name = @0 **&&** Email = @1)
| **Or** | FindByName**Or**Email | where (Name = @0 **\|\|** Email = @1)

#### Sample

```csharp
public interface IPersonRepository : IEntityRepository<Person, int>
{
    IEnumerable<Person> FindByLastName(string name);
    IEnumerable<Person> FindByBirthDateBetween(DateTime startDate, DateTime endDate);
    IEnumerable<Person> FindByFirstNameLikeAndActive(string name, bool active = true);
    IEnumerable<Person> FindByEmailLikeOrPhoneNotNull(string email);
    IEnumerable<Person> FindByFirstNameAndLastNameOrBirthDateGreaterThan(string firstName, string lastName, DateTime registerDate);
}

DbContext dbContext = CreateDbContext();
var repositoryFactory = new EntityRepositoryFactory();

var personRepository = factory.GetRepository<IPersonRepository>(dbContext);

var persons = personRepository.FindByLastName("A Last Name");
persons = personRepository.FindByBirthDateBetween(new DateTime(2015, 1, 1), new DateTime(2020, 1, 1));
persons = personRepository.FindByFirstNameLikeAndActive(string name, bool active = true);
persons = personRepository.FindByEmailLikeOrPhoneNotNull(string email);
persons = personRepository.FindFindByFirstNameAndLastNameOrBirthDateGreaterThan("A First Name", "A Last Name", new DateTime(2019, 1, 1));
```

### Using Customized Methods

Customized Methods can be defined as normal class:

- The method should be defined in the repository interface.
- The class with implementation may or may not implement the interface.
- This can be used for any kind of customization that you want.

```csharp
public interface IPersonRepository : IEntityRepository<Person, int>
{
    // Query Methods
    IEnumerable<Person> FindByEmailNotNull();
    IEnumerable<Person> FindByFirstNameStartingWith(string firstName);

    // Customized Methods
    void AddAll(IEnumerable<Person> persons);
}

public class PersonRepository : EntityRepository<Person, int>
{
    public void AddAll(IEnumerable<Person> persons)
    {
        foreach (var person in persons)
        {
            this.Insert(person)
        }
    }
}

// Getting Repository
DbContext dbContext = CreateDbContext();
var repositoryFactory = new EntityRepositoryFactory();

var personRepository = factory.GetRepository<IPersonRepository>(() => new PersonRepository(dbContext));

// Using Query Methods
var persons = personRepository.FindByEmailNotNull();
persons = personRepository.FindByFirstNameStartingWith("Name");

// Using Customized Methods
person1 = new Person() {...};
person2 = new Person() {...};
person3 = new Person() {...};

personRepository.AddAll(new List<Person> { person1, person2, person3 });
```

## Performance

The following metrics show how long it takes to execute top CRUD statements on a SQLServer database, comparing the provider to dry Entity Framework Core.

The benchmarks can be found at [DataQI.EntityFrameworkCore.Benchmarks](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/tree/main/test/DataQI.EntityFrameworkCore.Benchmarks) (contributions welcome!) and can be run via:

```bash
dotnet run --project .\test\DataQI.EntityFrameworkCore.Benchmarks\DataQI.EntityFrameworkCore.Benchmarks.csproj  -f net6.0 -c Release
```

```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.3930/22H2/2022Update)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.405
  [Host] : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT AVX2
  Dry    : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT AVX2

```

| Lib         | Method               | Note                     | Op Count  | Mean      | StdDev    | Error     | Gen0      | Gen1     | Gen2     | Allocated  |
|-------------|----------------------|------------------------- | ---------:|----------:|----------:|----------:|----------:|---------:|---------:|-----------:|
| Pure EfCore | FindAll&lt;T&gt;     | Select ~10,000 rows / op |    10,000 | 23.180 ms | 2.5252 ms | 2.1928 ms |  738.0000 | 278.0000 | 118.0000 | 3872.01 KB |
| DataQI      | FindAll&lt;T&gt;     | Select ~10,000 rows / op |    10,000 | 23.538 ms | 3.4966 ms | 3.0363 ms |  742.0000 | 272.0000 | 108.0000 | 3954.12 KB |
| Pure EfCore | FindOne&lt;T&gt;     | Select 1 row / op        |    10,000 |  1.161 ms | 0.3173 ms | 0.2755 ms |    2.0000 |        - |        - |   13.93 KB |
| DataQI      | FindOne&lt;T&gt;     | Select 1 row / op        |    10,000 |  1.168 ms | 0.3198 ms | 0.2777 ms |    2.0000 |        - |        - |   13.98 KB |
| Pure EfCore | CustomQuery&lt;T&gt; | Select 1 row / op        |    10,000 | 10.372 ms | 0.5653 ms | 0.4909 ms |    2.0000 |        - |        - |   13.34 KB |
| DataQI      | CustomQuery&lt;T&gt; | Select 1 row / op        |    10,000 | 12.232 ms | 0.4576 ms | 0.3973 ms |   18.0000 |   2.0000 |        - |  112.02 KB |
| Pure EfCore | Insert&lt;T&gt;      | Insert 1 row / op        |    10,000 | 15.053 ms | 4.9835 ms | 4.3275 ms | 1886.0000 |   4.0000 |        - | 7704.21 KB |
| DataQI      | Insert&lt;T&gt;      | Insert 1 row / op        |    10,000 | 15.220 ms | 5.4845 ms | 4.7625 ms | 1886.0000 |   4.0000 |        - | 7704.29 KB |
| Pure EfCore | Update&lt;T&gt;      | Update 1 row / op        |    10,000 | 13.906 ms | 3.5626 ms | 3.0936 ms | 1884.0000 |   4.0000 |        - | 7700.69 KB |
| DataQI      | Update&lt;T&gt;      | Update 1 row / op        |    10,000 | 16.803 ms | 4.8685 ms | 4.2276 ms | 1888.0000 |   6.0000 |        - | 7715.53 KB |
| Pure EfCore | Delete&lt;T&gt;      | Delete 1 row / op        |    10,000 |  9.193 ms | 3.7906 ms | 3.2916 ms |    6.0000 |        - |        - |   25.34 KB |
| DataQI      | Delete&lt;T&gt;      | Delete 1 row / op        |    10,000 |  9.195 ms | 0.4882 ms | 0.4239 ms |    6.0000 |        - |        - |   24.54 KB |

## Limitations and caveats

The DataQI EntityFrameworkCore Provider library is not an ORM or it attempts to solve all data persistence problems. It provides a structure based on Repository Pattern that facilitates the rapid creation of repositories with methods that allow the creation, modification and deletion of data, as well as the preparation of simple queries by signing the methods declared in an interface, in order to avoid most of the effort involved in writing standard code in projects that use the [EntityFrameworkCore](https://github.com/dotnet/efcore) library.

## Release Notes

**v3.1.0 - 2023/01**

- New! Added project to perform benchmarks
- New! Added support to register repositories for Microsoft's Dependency Injection
- New! Added sample project

**v3.0.0 - 2022/06**

- Change! Updated minimum version of supported `EntityFrameworkCore` to the lastest 6th major version

**v2.1.0 - 2022/06**

- Change! Updated minimum version of supported `EntityFrameworkCore` to the lastest 5th major version

**v2.0.0 - 2022/06**

- Change! Added support to the `EntityFrameworkCore` latest 5th major version

**v1.2.0 - 2022/06**

- Change! Added support to the `EntityFrameworkCore` latest 3rd major version
- Change! `EntityRepository` sets `EntityEntry.State` explicity to `EntityState.Modified` when change an `Entity`

**v1.1.0 - 2022/01**

- New! Added support to the new `RepositoryFactory` features
- New! Added capability to invokes non-standard methods defined on client
- Change! `TEntity` requirements on generic interface `IEntityRepository`
- **Breaking Change!** Removed `DbContext` as argument on `EntityRepositoryFactory` constructor

**v1.0.0 - 2020/09**

- Provided initial core base

## License

DataQI EntityFrameworkCore is released under the [MIT License](https://opensource.org/licenses/MIT).
