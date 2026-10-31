# DataQI EntityFrameworkCore

DataQI EntityFrameworkCore creates repository implementations from C# interfaces using [EF Core](https://github.com/dotnet/efcore) and [DataQI.Commons](https://github.com/henrique-gouveia/DataQI.Commons). It provides CRUD operations, queries derived from method names, criteria, LINQ predicates, and custom repository methods.

[![Build](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/actions/workflows/dotnet.yml/badge.svg)](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.EntityFrameworkCore/branch/main/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.EntityFrameworkCore)
[![NuGet](https://img.shields.io/nuget/v/DataQI.EntityFrameworkCore.svg)](https://www.nuget.org/packages/DataQI.EntityFrameworkCore/)
[![License](https://img.shields.io/github/license/henrique-gouveia/DataQI.EntityFrameworkCore.svg)](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/blob/main/LICENSE)

## Features

- Synchronous and asynchronous CRUD operations backed by EF Core, with cancellation support.
- Repositories declared as C# interfaces, with query methods derived from method names.
- Criteria with comparisons, text matches, logical groups, and ordering.
- Collection and single-entity queries, plus paging with `ISlice<T>` and `IPage<T>`.
- Deterministic paging order with primary-key tie-breaks and total counts only when needed.
- LINQ predicates and query builders for custom queries over `IQueryable<TEntity>`.
- Repository factories, custom implementations, and Microsoft dependency injection integration.
- Query-method completion through the transitive DataQI.Analyzers dependency.

## Getting Started

### Installing

Install the package in your application and the EF Core database provider for your database:

```bash
dotnet add package DataQI.EntityFrameworkCore
```

### Defining a Repository

The provider combines the independent `ICrudRepository<TEntity, TId>` and `IPagedRepository<TEntity, TId>` contracts. Extend `IEntityRepository<TEntity, TId>` from `DataQI.EntityFrameworkCore.Repository`. `TEntity` is the mapped entity class; `TId` is the type of its single primary key. Identifier methods take that key value, for example `FindOne(1)` and `Delete(person.Id)`. Composite keys are not supported by these identifier methods.

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
    public bool Active { get; set; }
}

public interface IPersonRepository : IEntityRepository<Person, int> { }
```

### Instancing a Repository

You must use an instance of the `EntityRepositoryFactory` class to instantiate a repository, located in the `DataQI.EntityFrameworkCore.Repository.Support` namespace, which requires a `DbContext` to make its calls:

```csharp
DbContext dbContext = CreateDbContext();
var repositoryFactory = new EntityRepositoryFactory();

var personRepository = repositoryFactory.GetRepository<IPersonRepository>(dbContext);
```

Disposing a repository disposes its context.

### Configuring by using Microsoft's Dependency Injection

After you have installed this library, if you're using a .NET web application, you can do the following:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

Don't forget to configure your `DbContext` (here, `PeopleContext` stands for your application's context):

```csharp
// Configure DbContext like you prefer...
builder.Services.AddDbContext<PeopleContext>(...);
```

If you don't need a specific repository interface, you can just register a service by doing the following:

```csharp
// It'll add IEntityRepository<Entity, int> repository service...
builder.Services.AddDefaultEntityRepository<Person, int, PeopleContext>();
```

If you need a specific repository interface, you can register it by doing the following:

```csharp
// It'll add IPersonRepository repository service...
builder.Services.AddEntityRepository<IPersonRepository, PeopleContext>();
```

Now, if you need to add custom support using a repository implementation, you can register it by doing the following:

```csharp
// It'll add IPersonRepository repository service supported by SomeCustomRepository ...
builder.Services.AddEntityRepository<IPersonRepository, SomeCustomRepository, PeopleContext>();
```

The repository and `EntityRepositoryFactory` are registered as scoped services with `TryAdd` semantics, preserving existing registrations, and `AddDbContext` is scoped by default. Resolve the repository within a scope and await each operation before starting another on the same context.

Let the scope manage the lifetime of the context: disposing a repository by hand also affects other services sharing it. See the [EF Core context lifetime documentation](https://learn.microsoft.com/ef/core/dbcontext-configuration/) and take a look at the [Samples](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/tree/main/samples) to see how this works.

### Using Default Methods

| Operation | Synchronous / asynchronous methods | Input |
| --- | --- | --- |
| Insert | `Insert` / `InsertAsync` | Entity |
| Insert or update | `Save` / `SaveAsync` | Entity |
| Delete | `Delete` / `DeleteAsync` | `TId` |
| Check existence | `Exists` / `ExistsAsync` | `TId` |
| Find by identifier | `FindOne` / `FindOneAsync` | `TId` |
| Find one by criteria | `FindOne` / `FindOneAsync` | Criteria builder |
| Find many | `Find` / `FindAsync` | Criteria, predicate, or LINQ builder |
| Find all | `FindAll` / `FindAllAsync` | None |

Write operations register changes in the context. Call `SaveChanges` or `SaveChangesAsync` to persist them. `Save` inserts when the key does not exist; otherwise it copies values to the existing entity and marks it modified. Delete requires an existing identifier.

```csharp
personRepository.Insert(person);
context.SaveChanges();
person.Active = false;
await personRepository.SaveAsync(person, cancellationToken);
await context.SaveChangesAsync(cancellationToken);
var allPeople = await personRepository.FindAllAsync(cancellationToken);
var onePerson = personRepository.FindOne(person.Id);
var exists = personRepository.Exists(person.Id);
await personRepository.DeleteAsync(person.Id, cancellationToken);
await context.SaveChangesAsync(cancellationToken);
```

Identifier lookups return tracked entities and can return an entity already in the change tracker. Criteria, query methods, predicate searches, and `FindAll` return entities without tracking. `Find()` and LINQ query builders use the context's default tracking behavior; add `AsNoTracking()` when appropriate.

### Using Query Methods

Declare query methods on a repository interface and call them with values. The [Commons query method reference](https://github.com/henrique-gouveia/DataQI.Commons/blob/main/README.md#query-methods) defines aliases, argument counts, precedence, reserved property names, ordering, and supported return types.

#### Supported keywords inside method names

Equality is the default operator. Matching is case-sensitive, and values are consumed left to right. The provider builds expression trees that EF Core translates for the selected database. The table shows equivalent C# predicates, where `p` is the entity and `value`, `start`, `end`, and `values` are supplied arguments.

| Keyword | Other forms | Args | Equivalent predicate |
| --- | --- | ---: | --- |
| *(none)* / `Equal` | `Equals`, `IsEqual`, `IsEquals` | 1 | `p.FirstName == value` |
| `Not` + keyword | negation marker (`NotEqual`, `NotBetween`, `NotIn`, `NotNull`, `NotLike`...) | same | negates the corresponding predicate, e.g. `!(p.FirstName == value)` |
| `Between` | `IsBetween` | 2 | `p.BirthDate >= start && p.BirthDate <= end` |
| `GreaterThan` / `GreaterThanEqual` | `IsGreaterThan` / `IsGreaterThanEqual` | 1 | `p.BirthDate > value` / `p.BirthDate >= value` |
| `LessThan` / `LessThanEqual` | `IsLessThan` / `IsLessThanEqual` | 1 | `p.BirthDate < value` / `p.BirthDate <= value` |
| `In` | `IsIn` | 1 | `values.Contains(p.LastName)` |
| `Null` | `IsNull` | 0 | `p.Email == null` |
| `StartingWith` | `StartsWith`, `IsStartingWith` | 1 | `p.FirstName.StartsWith(value)` |
| `EndingWith` | `EndsWith`, `IsEndingWith` | 1 | `p.FirstName.EndsWith(value)` |
| `Containing` | `Contains`, `IsContaining` | 1 | `p.FirstName.Contains(value)` |
| `Like` | `IsLike` | 1 | `p.FirstName.Contains(value)` |
| `And` / `Or` | | 0 | `left && right` / `left \|\| right` |
| `OrderBy`, `Asc`, `Desc` | | 0 | ordering (see [Ordering Query Results](#ordering-query-results)) |

Text arguments are literal strings: `Like("Ada")` and `Containing("Ada")` both perform substring searches. `%` is literal here. The Dapper provider uses SQL `LIKE` patterns instead, so text arguments are not portable unchanged. Case sensitivity and supported translations depend on the database provider and collation. String relational comparisons use `string.Compare(property, value)` against zero.

`Null` and `NotNull` require a reference or nullable property; applying them to a non-nullable value type throws `InvalidOperationException`. For `In`, use an `object[]` (box numeric values, for example `new object[] { 1, 2 }`) or a covariant reference array such as `string[]`; the shared parser does not accept `int[]` or lists.

With EF Core's default null semantics, negating equality to a non-null value includes rows where the property is null. For example, `FindByEmailNotEqual("ada@example.com")` also matches null email values. Dapper's SQL negation excludes those rows. EF Core's `UseRelationalNulls` configuration changes this behavior; add an explicit `NotNull` condition when the query must exclude nulls. See [EF Core null comparisons](https://learn.microsoft.com/ef/core/querying/null-comparisons).

#### Sample

```csharp
public interface IPersonRepository : IEntityRepository<Person, int>
{
    IEnumerable<Person> FindByLastName(string lastName);
    IEnumerable<Person> FindByBirthDateBetween(DateTime start, DateTime end);
    IEnumerable<Person> FindByFirstNameContainingAndActive(string name, bool active);
    IEnumerable<Person> FindByEmailContainingOrPhoneNotNull(string email);
    IEnumerable<Person> FindByFirstNameAndLastNameOrBirthDateGreaterThan(
        string firstName, string lastName, DateTime birthDate);
}
```

```csharp
var people = personRepository.FindByLastName("Lovelace");
people = personRepository.FindByBirthDateBetween(new DateTime(1800, 1, 1), new DateTime(1900, 1, 1));
people = personRepository.FindByFirstNameContainingAndActive("Ada", true);
people = personRepository.FindByEmailContainingOrPhoneNotNull("@example.com");
people = personRepository.FindByFirstNameAndLastNameOrBirthDateGreaterThan(
    "Ada", "Lovelace", new DateTime(1900, 1, 1));
```

#### Paging

Added to `IProductRepository` (import `DataQI.Commons.Repository.Paging` and `DataQI.Commons.Repository.Paging.Support`):

```csharp
ISlice<Product> FindByDepartmentOrderByNameAsc(string department, IPageRequest page);
Task<IPage<Product>> FindByDepartmentAsync(
    string department, IPageRequest page, CancellationToken cancellationToken = default);
```

```csharp
var request = PageRequest.Of(0, 20, Order.Asc(nameof(Product.Name)));
var slice = repository.FindSlice(c => c
    .Add(Restrictions.Equal(nameof(Product.Department), "D1")), request);
var page = repository.FindPage(c => c, request);
var namedSlice = repository.FindByDepartmentOrderByNameAsc("D1", request);
var namedPage = await repository.FindByDepartmentAsync("D1", request, cancellationToken);
var asyncSlice = await repository.FindSliceAsync(c => c, request, cancellationToken);
```

`PageIndex` is zero-based. `ISlice<T>` includes read-only `Items`, `HasNext` and `HasPrevious`; `IPage<T>` adds `long TotalCount` and `long TotalPages`. Slice uses one query and is the recommended default. Page runs `COUNT` only when needed: a partial page with items, or an empty first page, reveals the total. An empty page beyond the end still counts matching rows.

Page items and total can come from two non-atomic queries; wrap the operation in a transaction if consistency matters. Clients should cap `PageSize`; deep `OFFSET` degrades performance. Requests require a positive size below `int.MaxValue` and an offset no larger than `int.MaxValue`; a null request throws `ArgumentException`.

Ordering follows method-name `OrderBy`, criteria `AddOrder`, then `page.Orders`, keeping the first occurrence of each property. Missing primary-key properties are appended ascending for stable ties. `NULL` ordering depends on the database. Paging rejects unknown order properties with `InvalidOperationException` naming the entity and property. An entity without a key needs an explicit order.

Paging query methods require exactly one `IPageRequest` as the last non-token parameter and a matching slice/page return type. An optional `CancellationToken` belongs last; async methods forward it to the provider. Invalid parsed-query signatures fail at proxy creation. See the [Commons query contracts](https://github.com/henrique-gouveia/DataQI.Commons#paging) for the full rules.

EF Core forwards `CancellationToken` to asynchronous reads and counts. Shadow and composite primary keys also complete the ordering.

### Using Async Query Methods

Return `Task<IEnumerable<Person>>` or `Task<Person>` to run a query asynchronously. The return type selects synchronous or asynchronous execution; a trailing `Async` in the name is optional. A final `CancellationToken` is forwarded to asynchronous EF Core queries.

```csharp
// Added to IPersonRepository
Task<IEnumerable<Person>> FindByLastNameAsync(
    string lastName, CancellationToken cancellationToken = default);
```

```csharp
var people = await personRepository.FindByLastNameAsync("Lovelace", cancellationToken);
```

### Using Single-Entity Query Methods

A query method can return `Person` or `Task<Person>`. Zero matches returns `null`; multiple matches throw `InvalidOperationException`, including when ordered. Return `IEnumerable<Person>` for collections.

```csharp
// Added to IPersonRepository
Person FindByEmail(string email);
Task<Person> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
```

```csharp
var person = await personRepository.FindByEmailAsync("ada@example.com", cancellationToken);
```

### Ordering Query Results

`OrderBy` follows at least one filter condition. Specify `Asc` or `Desc` after each non-final sort property; the final direction defaults to ascending. For example, the method below sorts by first name ascending, then birth date descending.

```csharp
// Added to IPersonRepository
Task<IEnumerable<Person>> FindByLastNameOrderByFirstNameAscBirthDateDescAsync(
    string lastName, CancellationToken cancellationToken = default);
```

```csharp
var ordered = await personRepository.FindByLastNameOrderByFirstNameAscBirthDateDescAsync(
    "Lovelace", cancellationToken);
```

### Query-method completion

Completion is provided by [DataQI.Analyzers](https://github.com/henrique-gouveia/DataQI.Analyzers), which comes with the Commons 4.0 dependency (see the [Commons reference](https://github.com/henrique-gouveia/DataQI.Commons/blob/main/README.md#completing-query-methods-with-dataqianalyzers)). See the DataQI.Analyzers repository for compatible IDEs and editor configuration.

Specific to this provider: relational comparisons and `Between` are suggested for enum properties, but this provider cannot translate them; use equality or `In` instead. The [shared grammar](https://github.com/henrique-gouveia/DataQI.Commons/blob/main/README.md#query-methods) and the provider's translation rules determine which queries work.

### Using Linq Query and Expression Builders

Compose queries using `Find()`, a query builder, or a strongly typed predicate. Use EF Core's async terminal operators when composing directly from `Find()`.

```csharp
var first = personRepository.Find()
    .Where(p => p.FirstName.Contains("Ada") && p.Active)
    .FirstOrDefault();

var page = personRepository.Find(query => query
    .Where(p => p.BirthDate >= new DateTime(1800, 1, 1)
        && p.BirthDate <= new DateTime(1900, 1, 1))
    .OrderBy(p => p.Id)
    .Skip(0)
    .Take(20));

var asyncPage = await personRepository.FindAsync(query => query
    .Where(p => p.Active)
    .OrderBy(p => p.Id)
    .Skip(0)
    .Take(20), cancellationToken);

var matches = personRepository.Find(p => p.FirstName.Contains("Ada")
    && (p.BirthDate <= new DateTime(1900, 1, 1) || p.Active));
var asyncMatches = await personRepository.FindAsync(
    p => p.LastName == "Lovelace", cancellationToken);
var firstAsync = await personRepository.Find()
    .OrderBy(p => p.Id)
    .FirstOrDefaultAsync(cancellationToken);
```

### Using Criteria Definitions

Use `Restrictions` and `Order` from `DataQI.Commons.Query.Support`. Top-level criteria are combined with `And`; use `Disjunction` for an explicit `Or` group.

```csharp
var people = personRepository.Find(criteria => criteria
    .Add(Restrictions.Containing(nameof(Person.FirstName), "Ada"))
    .Add(Restrictions.Disjunction()
        .Add(Restrictions.Between(nameof(Person.BirthDate),
            new DateTime(1800, 1, 1), new DateTime(1900, 1, 1)))
        .Add(Restrictions.Equal(nameof(Person.Active), true)))
    .AddOrder(Order.Asc(nameof(Person.LastName)))
    .AddOrder(Order.Desc(nameof(Person.BirthDate))));

var asyncPeople = await personRepository.FindAsync(criteria => criteria
    .Add(Restrictions.Containing(nameof(Person.LastName), "Love"))
    .Add(Restrictions.Not(Restrictions.Null(nameof(Person.Email))))
    .AddOrder(Order.Asc(nameof(Person.FirstName))), cancellationToken);

var onePerson = await personRepository.FindOneAsync(criteria => criteria
    .Add(Restrictions.Equal(nameof(Person.Email), "ada@example.com")), cancellationToken);
```

#### Enum values in criteria

Equality and membership criteria accept enum values, numeric values, and numeric strings such as `"1"`, including for nullable enum properties. String member names such as `"Active"` throw `FormatException`; pass the enum value instead. Numeric conversion uses invariant culture and the enum's underlying type. Values need not name a declared member; out-of-range values throw `OverflowException`.

`GreaterThan`, `GreaterThanEqual`, `LessThan`, `LessThanEqual`, and `Between` do not support enum or `bool` properties: constructing these comparisons throws `InvalidOperationException`. Use equality or membership criteria for these types.

### Using Customized Methods

Declare a custom method on an interface and supply a class with a matching public method. The class can inherit `EntityRepository<Person, int>` without implementing the custom interface; the factory combines its methods with the interface's derived queries.

```csharp
public interface ICustomPersonRepository : IEntityRepository<Person, int>
{
    IEnumerable<Person> FindByEmailNotNull();
    void AddAll(IEnumerable<Person> people);
}

public class PersonRepository : EntityRepository<Person, int>
{
    public PersonRepository(DbContext context) : base(context) { }

    public void AddAll(IEnumerable<Person> people)
    {
        foreach (var person in people)
        {
            Insert(person);
        }
    }
}
```

Pass the custom implementation to the factory:

```csharp
var repositoryFactory = new EntityRepositoryFactory();
var personRepository = repositoryFactory.GetRepository<ICustomPersonRepository>(
    () => new PersonRepository(context));
personRepository.AddAll(new[]
{
    new Person { FirstName = "Ada", LastName = "Lovelace" },
    new Person { FirstName = "Grace", LastName = "Hopper" }
});
context.SaveChanges();
var people = personRepository.FindByEmailNotNull();
```

## Performance

These historical SQL Server measurements compare DataQI with direct EF Core calls. They predate the 6.0 expression-based criteria rewrite and do not measure the current code or dependencies.

The benchmarks can be found at [DataQI.EntityFrameworkCore.Benchmarks](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/tree/main/test/DataQI.EntityFrameworkCore.Benchmarks) (contributions welcome!) and can be run via:

```bash
dotnet run --project .\test\DataQI.EntityFrameworkCore.Benchmarks\DataQI.EntityFrameworkCore.Benchmarks.csproj  -f net10.0 -c Release
```

The recorded environment was BenchmarkDotNet 0.13.12 and .NET 6.0.3; the benchmark project now targets .NET 10.

```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.3930/22H2/2022Update)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.405
  [Host] : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT AVX2
  Dry    : .NET 6.0.3 (6.0.322.12309), X64 RyuJIT AVX2

```

| Lib         | Method               | Note                     | Op Count |      Mean |    StdDev |     Error |      Gen0 |     Gen1 |     Gen2 |  Allocated |
|-------------|----------------------|--------------------------|---------:|----------:|----------:|----------:|----------:|---------:|---------:|-----------:|
| Pure EfCore | FindAll&lt;T&gt;     | Select ~10,000 rows / op |   10,000 | 23.180 ms | 2.5252 ms | 2.1928 ms |  738.0000 | 278.0000 | 118.0000 | 3872.01 KB |
| DataQI      | FindAll&lt;T&gt;     | Select ~10,000 rows / op |   10,000 | 23.538 ms | 3.4966 ms | 3.0363 ms |  742.0000 | 272.0000 | 108.0000 | 3954.12 KB |
| Pure EfCore | FindOne&lt;T&gt;     | Select 1 row / op        |   10,000 |  1.161 ms | 0.3173 ms | 0.2755 ms |    2.0000 |        - |        - |   13.93 KB |
| DataQI      | FindOne&lt;T&gt;     | Select 1 row / op        |   10,000 |  1.168 ms | 0.3198 ms | 0.2777 ms |    2.0000 |        - |        - |   13.98 KB |
| Pure EfCore | CustomQuery&lt;T&gt; | Select 1 row / op        |   10,000 | 10.372 ms | 0.5653 ms | 0.4909 ms |    2.0000 |        - |        - |   13.34 KB |
| DataQI      | CustomQuery&lt;T&gt; | Select 1 row / op        |   10,000 | 12.232 ms | 0.4576 ms | 0.3973 ms |   18.0000 |   2.0000 |        - |  112.02 KB |
| Pure EfCore | Insert&lt;T&gt;      | Insert 1 row / op        |   10,000 | 15.053 ms | 4.9835 ms | 4.3275 ms | 1886.0000 |   4.0000 |        - | 7704.21 KB |
| DataQI      | Insert&lt;T&gt;      | Insert 1 row / op        |   10,000 | 15.220 ms | 5.4845 ms | 4.7625 ms | 1886.0000 |   4.0000 |        - | 7704.29 KB |
| Pure EfCore | Update&lt;T&gt;      | Update 1 row / op        |   10,000 | 13.906 ms | 3.5626 ms | 3.0936 ms | 1884.0000 |   4.0000 |        - | 7700.69 KB |
| DataQI      | Update&lt;T&gt;      | Update 1 row / op        |   10,000 | 16.803 ms | 4.8685 ms | 4.2276 ms | 1888.0000 |   6.0000 |        - | 7715.53 KB |
| Pure EfCore | Delete&lt;T&gt;      | Delete 1 row / op        |   10,000 |  9.193 ms | 3.7906 ms | 3.2916 ms |    6.0000 |        - |        - |   25.34 KB |
| DataQI      | Delete&lt;T&gt;      | Delete 1 row / op        |   10,000 |  9.195 ms | 0.4882 ms | 0.4239 ms |    6.0000 |        - |        - |   24.54 KB |

## Release Notes

**v6.0.0 - upcoming release**

- New! Add slice and page queries through criteria or query methods, with stable ordering and a count query only when the total cannot be deduced.
- **Breaking Change!** `IEntityRepository<TEntity, TId>` now combines the independent CRUD and paging contracts; custom implementations must add `FindSlice`, `FindSliceAsync`, `FindPage` and `FindPageAsync`.
- **Breaking Change!** Adopted the Commons 4.x criteria AST and removed the old public criteria and command-building types listed in the migration guide.
- **Breaking Change!** Criteria-based `Find` now throws `InvalidOperationException` when an order names an unmapped property.
- Change! Replaced Dynamic LINQ criteria strings with typed predicates and ordering expressions.
- Change! Null tests on non-nullable properties now throw a clear exception.
- New! Added XML documentation for public APIs.
- Change! The library targets .NET 6 with EF Core 6.0.36; tests, samples, and benchmarks use .NET 10 and EF Core 10.0.13.

**v5.0.0 - 2026/10**

- New! Added support for async query methods
- New! Added single-entity query methods and criteria-based `FindOne`/`FindOneAsync` methods
- New! Added ordering through criteria and the `OrderBy` suffix in query method names
- Change! Adopted the new `DataQI.Commons` APIs for async query methods, single-entity queries and ordering

**v4.0.0 - 2024/12**

- New! Added support for queries using LINQ query and expression builders
- Change! Upgraded `DataQI.Commons` to `2.0.0` to support new features

**v3.1.0 - 2023/01**

- New! Added a benchmark project
- New! Added support for registering repositories with Microsoft's dependency injection container
- New! Added a sample project

**v3.0.0 - 2022/06**

- Change! Updated the minimum supported EF Core version to 6.x

**v2.1.0 - 2022/06**

- Change! Updated the minimum supported EF Core version to 5.x

**v2.0.0 - 2022/06**

- Change! Added support for EF Core 5.x

**v1.2.0 - 2022/06**

- Change! Added support for EF Core 3.x
- Change! `EntityRepository` sets `EntityEntry.State` explicitly to `EntityState.Modified` when changing an entity

**v1.1.0 - 2022/01**

- New! Added support for the new `RepositoryFactory` features
- New! Added support for invoking custom methods defined by consumers
- Change! Updated `TEntity` requirements on the generic `IEntityRepository` interface
- **Breaking Change!** Removed the `DbContext` argument from the `EntityRepositoryFactory` constructor

**v1.0.0 - 2020/09**

- Initial release

## License

DataQI EntityFrameworkCore is released under the [MIT License](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore/blob/main/LICENSE).
