using Microsoft.EntityFrameworkCore;

namespace DataQI.EntityFrameworkCore.Benchmarks;

[Description("Pure EfCore")]
public class EntityFrameworkCoreBenchmark : BenchmarkBase
{
    [GlobalSetup]
    public void Setup() => InsertInitialEntitiesUsingAdoNet();

    [GlobalCleanup]
    public void Cleanup() => DeleteAllEntitiesUsingAdoNet();
    
    [Benchmark(Description = "FindAll<T>")]
    public IList<Entity> FindAll()
    {
        PrepareStep();
        
        var entities = Context.Entities!.AsNoTracking().ToList();
        entities.Should().HaveCount(InsertedEntities.Count);
        return entities;
    }
    
    [Benchmark(Description = "FindOne<T>")]
    public Entity? FindOne()
    {
        PrepareStep();
    
        var entityToFind = CurrentStepEntity;
        var entity = Context.Find<Entity>(entityToFind.Id);
        entity.Should().NotBe(null);
        return entity;
    }
    
    [Benchmark(Description = "CustomQuery<T>")]
    public IList<Entity> CustomQuery()
    {
        PrepareStep();
    
        var entityToQuery = CurrentStepEntity;
        var entities = Context.Entities!
            .Where(entity => entity.Name == entityToQuery.Name)
            .ToList();
        entities
            .Should().HaveCount(1)
            .And.Subject.FirstOrDefault()!.Name
            .Should().BeEquivalentTo(entityToQuery.Name);
        return entities;
    }
    
    [Benchmark(Description = "Insert<T>")]
    public Entity Insert()
    {
        PrepareStep();
    
        var entity = NewEntity();
        Context.Add(entity);
        Context.SaveChanges();
        return entity;
    }
    
    [Benchmark(Description = "Update<T>")]
    public Entity Update()
    {
        PrepareStep();
    
        var entityToUpdate = CurrentStepEntity;
        entityToUpdate.Name = $"{entityToUpdate.Name} - Updated";
        Context.Update(entityToUpdate);
        Context.SaveChanges();
        return entityToUpdate;
    }
    
    [Benchmark(Description = "Delete<T>")]
    public Entity? Delete()
    {
        PrepareStep();

        var entity = CurrentStepEntity;
        var entityToDelete = Context.Find<Entity>(entity.Id);
        Context.Remove(entityToDelete!);
        Context.SaveChanges();
        return entityToDelete;
    }
}