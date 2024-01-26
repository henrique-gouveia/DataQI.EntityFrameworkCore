var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<ApiDbContext>(options => options
    .UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// DataQI - If you are using version 3.0.x or lower, so you can do that...
builder.Services.AddScoped<EntityRepositoryFactory>();

builder.Services.AddScoped(sp =>
{
    var dbContext = sp.GetRequiredService<ApiDbContext>();
    var repositoryFactory = sp.GetRequiredService<EntityRepositoryFactory>();
    // Using the below way for customization support!
    return repositoryFactory.GetRepository<IEntityRepository>(() => new UnitOfWorkRepository<Entity, int>(dbContext));
});

// DataQI - If you are using version 3.1.x or higher, so you can do that...

// It'll add IEntityRepository<Entity, int> repository service...
// builder.Services.AddDefaultEntityRepository<Entity, int, ApiDbContext>();

// It'll add IEntityRepository repository service...
// builder.Services.AddEntityRepository<IEntityRepository, ApiDbContext>();

// It'll add IEntityRepository repository service supported by EntityRepository<Entity, int>...
// builder.Services.AddEntityRepository<IEntityRepository, EntityRepository<Entity, int>, ApiDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Routes
app.MapRoutes();

app.Run();