var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
builder.Services.AddDbContext<ApiDbContext>(options => options
    .UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// DataQI
builder.Services.AddScoped<EntityRepositoryFactory>();

builder.Services.AddScoped(sp =>
{
    var dbContext = sp.GetRequiredService<ApiDbContext>();
    var repositoryFactory = sp.GetRequiredService<EntityRepositoryFactory>();
    // Using the below way for customization support!
    return repositoryFactory.GetRepository<IEntityRepository>(() => new UnitOfWorkRepository<Entity, int>(dbContext));
});

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