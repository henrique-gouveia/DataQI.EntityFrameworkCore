namespace SimpleApi;

public class ApiDbContext : DbContext, IUnitOfWork
{
    public ApiDbContext(DbContextOptions options) : base(options)
        => EnsureDatabaseCreated();
    private void EnsureDatabaseCreated() => Database.EnsureCreated();

    public async Task<bool> CompleteAsync() => await SaveChangesAsync() > 0;
    
    public DbSet<Entity> Entities { get; set; } = null!;
}