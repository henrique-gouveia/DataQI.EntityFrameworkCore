namespace SimpleApi;

public static class RoutesExtensions
{
    public static void MapRoutes(this WebApplication app)
    {
        // All
        app.MapGet("api/entities", async (
            [FromServices] IEntityRepository entityRepository,
            string? name) =>
        {
            IEnumerable<Entity> entities;
            if (string.IsNullOrEmpty(name))
                entities = await entityRepository.FindAllAsync();
            else
                entities = entityRepository.FindByNameStartingWith(name);
            return Results.Ok(entities);
        }).WithName("Search Entities");
        
        // ById
        app.MapGet("api/entities/{id:int}", async (
            [FromServices] IEntityRepository entityRepository,
            int id) =>
        {
            var entity = await entityRepository.FindOneAsync(id);
            return entity switch
            {
                null => Results.NotFound(),
                _ => Results.Ok(entity)
            };
        }).WithName("Find Entity");
        
        // Add
        app.MapPost("api/entities", async (
            [FromServices] ApiDbContext dbContext,
            [FromServices] IEntityRepository entityRepository,
            [FromBody] EntityToPersist entityToInsert) =>
        {
            Entity entity = entityToInsert;
            await entityRepository.InsertAsync(entity);
            await entityRepository.UnitOfWork.CompleteAsync();
            return Results.Ok(entity);
        }).WithName("Add Entity");
        
        // Update
        app.MapPut("api/entities/{id:int}", async (
            [FromServices] ApiDbContext dbContext,
            [FromServices] IEntityRepository entityRepository,
            int id,
            EntityToPersist entityToUpdate) =>
        {
            var entity = await entityRepository.FindOneAsync(id);
            if (entity is null) return Results.NotFound(); 
            entity.Name = entityToUpdate.Name;
            await entityRepository.SaveAsync(entity);
            await entityRepository.UnitOfWork.CompleteAsync();
            return Results.NoContent();
        }).WithName("Update Entity");
        
        // Delete
        app.MapDelete("api/entities/{id:int}", async (
            [FromServices] ApiDbContext dbContext,
            [FromServices] IEntityRepository entityRepository,
            int id) =>
        {
            var exists = await entityRepository.ExistsAsync(id); 
            if (!exists) return Results.NotFound();
            await entityRepository.DeleteAsync(id);
            await entityRepository.UnitOfWork.CompleteAsync();
            return Results.NoContent();
        }).WithName("Delete Entity");
    }
}

// ViewModels
public record EntityToPersist(string Name)
{
    public static implicit operator Entity(EntityToPersist entity) =>
        new() { Name = entity.Name };
}

// Domain Classes
public interface IEntityRepository : IEntityRepository<Entity, int>
{
    IUnitOfWork UnitOfWork { get; }
    IEnumerable<Entity> FindByNameStartingWith(string name);
}

[Table("entities")]
public class Entity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
    [Column("name")]
    public string? Name { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}