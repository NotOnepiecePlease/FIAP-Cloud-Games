using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CatalogDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CatalogDb") ?? "Data Source=catalog.db"));
builder.Services.AddSingleton<EventBus>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDb>();
    db.Database.EnsureCreated();
    if (!db.Games.Any())
    {
        db.Games.AddRange(
            new Game(Guid.NewGuid(), "Space FIAP", "Aventura espacial cooperativa", 79.90m, 50, DateTimeOffset.UtcNow),
            new Game(Guid.NewGuid(), "Cloud Racer", "Corrida arcade em nuvem", 59.90m, 80, DateTimeOffset.UtcNow));
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "fcg-catalog-api" }));
app.MapGet("/games", async (CatalogDb db) => Results.Ok(await db.Games.AsNoTracking().OrderBy(g => g.Title).ToListAsync()));
app.MapGet("/games/{id:guid}", async (Guid id, CatalogDb db) =>
    await db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id) is { } game ? Results.Ok(game) : Results.NotFound());
app.MapPost("/games", async (CreateGameRequest request, CatalogDb db, EventBus bus) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || request.Price < 0 || request.Stock < 0) return Results.BadRequest();
    var game = new Game(Guid.NewGuid(), request.Title.Trim(), request.Description.Trim(), request.Price, request.Stock, DateTimeOffset.UtcNow);
    db.Games.Add(game);
    await db.SaveChangesAsync();
    bus.Publish("catalog.game-created", game);
    return Results.Created($"/games/{game.Id}", game);
});
app.MapPut("/games/{id:guid}/stock", async (Guid id, UpdateStockRequest request, CatalogDb db, EventBus bus) =>
{
    var game = await db.Games.FindAsync(id);
    if (game is null) return Results.NotFound();
    if (request.Stock < 0) return Results.BadRequest();
    game.Stock = request.Stock;
    await db.SaveChangesAsync();
    bus.Publish("catalog.stock-updated", new { game.Id, game.Stock });
    return Results.Ok(game);
});

app.Run();

record CreateGameRequest(string Title, string Description, decimal Price, int Stock);
record UpdateStockRequest(int Stock);
class Game(Guid id, string title, string description, decimal price, int stock, DateTimeOffset createdAt)
{
    public Guid Id { get; set; } = id;
    public string Title { get; set; } = title;
    public string Description { get; set; } = description;
    public decimal Price { get; set; } = price;
    public int Stock { get; set; } = stock;
    public DateTimeOffset CreatedAt { get; set; } = createdAt;
}
class CatalogDb(DbContextOptions<CatalogDb> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Game>().HasKey(g => g.Id);
}
class EventBus(IConfiguration configuration)
{
    public void Publish(string routingKey, object message)
    {
        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(configuration["RabbitMq:Uri"] ?? "amqp://guest:guest@localhost:5672") };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare("fcg.events", ExchangeType.Topic, durable: true);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            channel.BasicPublish("fcg.events", routingKey, basicProperties: null, body: body);
        }
        catch { }
    }
}
