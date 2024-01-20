using Microsoft.EntityFrameworkCore;

namespace DataQI.EntityFrameworkCore.Benchmarks.Support;

public class EfCoreContext : DbContext
{
    protected EfCoreContext(DbContextOptions options) : base(options)
    {  }

    public static EfCoreContext NewInstance(SqlConnection connection)
    {
        var optionsBuilder = new DbContextOptionsBuilder().UseSqlServer(connection);
        var context = new EfCoreContext(optionsBuilder.Options);
        return context;
    }
    
    public DbSet<Entity>? Entities { get; private set; }
}