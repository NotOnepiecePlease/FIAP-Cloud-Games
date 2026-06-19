using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PaymentsDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PaymentsDb") ?? "Data Source=payments.db"));
builder.Services.AddSingleton<EventBus>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PaymentsDb>().Database.EnsureCreated();
}
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "fcg-payments-api" }));
app.MapGet("/payments", async (PaymentsDb db) => Results.Ok(await db.Payments.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync()));
app.MapPost("/payments", async (CreatePaymentRequest request, PaymentsDb db, EventBus bus) =>
{
    if (request.UserId == Guid.Empty || request.GameId == Guid.Empty || request.Amount <= 0) return Results.BadRequest();
    var status = request.Amount <= 500 ? "Approved" : "PendingReview";
    var payment = new Payment(Guid.NewGuid(), request.UserId, request.GameId, request.Amount, status, DateTimeOffset.UtcNow);
    db.Payments.Add(payment);
    await db.SaveChangesAsync();
    bus.Publish(status == "Approved" ? "payment.approved" : "payment.pending-review", payment);
    return Results.Created($"/payments/{payment.Id}", payment);
});
app.MapGet("/payments/{id:guid}", async (Guid id, PaymentsDb db) =>
    await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id) is { } payment ? Results.Ok(payment) : Results.NotFound());

app.Run();

record CreatePaymentRequest(Guid UserId, Guid GameId, decimal Amount);
record Payment(Guid Id, Guid UserId, Guid GameId, decimal Amount, string Status, DateTimeOffset CreatedAt);
class PaymentsDb(DbContextOptions<PaymentsDb> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Payment>().HasKey(p => p.Id);
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
