using Xunit;

namespace DataQI.EntityFrameworkCore.Test.Fixtures
{
    [CollectionDefinition(nameof(DatabaseCollection), DisableParallelization = true)]
    public class DatabaseCollection
    {
    }
}
