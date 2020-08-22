# DataQI EntityFrameworkCore [![NuGet](https://img.shields.io/nuget/v/DataQI.EntityFrameworkCore.svg)](https://www.nuget.org/packages/DataQI.EntityFrameworkCore/)

DataQI.EntityFrameworkCore is a [EntityFrameworkCore](https://github.com/dotnet/efcore) provider that use infraestructure provided by [DataQI.Commons](https://github.com/henrique-gouveia/DataQI.Commons) built around essential features of the C# 6 that to make it easy to implement repositories.

## Getting Started

DataQI EntityFrameworkCore is a library heavily inspired by Pivotal's Spring Data JPA library, and it turns your Data Repositories a live interface, making possible a custom Query Methods definitions:

```csharp
public interface IPersonRepository : IEntityRepository<Person>
{
    IEnumerable<Person> FindByFullName(string fullName);
  
    IEnumerable<Person> FindByEmailLike(string email);
  
    IEnumerable<Person> FindByDateRegisterBetween(DateTime startDate, DateTime endDate);
}
```

The `EntityRepositoryFactory` class extends `RepositoryFactory` to define abstract method `GetCustomImplementation` to generate an implementation of `IPersonRepository` that uses a Data Base Connection to make its calls:

```csharp
var repositoryFactory = new EntityRepositoryFactory(Connection);
personRepository = repositoryFactory.GetRepository<IPersonRepository>();
```

Visit [Use Default Methods](https://github.com/henrique-gouveia/DataQI.Commons#using-default-methods), [Using Criteria Definitions](https://github.com/henrique-gouveia/DataQI.Commons#using-criteria-definitions) and [Using Query Methods](https://github.com/henrique-gouveia/DataQI.Commons#using-query-methods) for examples to use `personRepository`.

## Limitations and caveats

DataQI does attempt to solve some problems. It is in the experimental phase.

## Contributors

* Henrique Gouveia
* Brenno Brandão

A [recent list of contributors](https://github.com/henrique-gouveia/DataQI.Commons/graphs/contributors) can always be obtained on GitHub.

## License

DataQI EntityFrameworkCore is released under the [MIT License](https://opensource.org/licenses/MIT).
