using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<NotificationsDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("NotificationsDb") ?? "Data Source=notifications.db"));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotificationsDb>().Database.EnsureCreated();
}
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "fcg-notifications-api" }));
app.MapGet("/notifications", async (NotificationsDb db) => Results.Ok(await db.Notifications.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToListAsync()));
app.MapPost("/notifications", async (CreateNotificationRequest request, NotificationsDb db) =>
{
    if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Message)) return Results.BadRequest();
    var notification = new Notification(Guid.NewGuid(), request.UserId, request.Channel.Trim(), request.Message.Trim(), false, DateTimeOffset.UtcNow);
    db.Notifications.Add(notification);
    await db.SaveChangesAsync();
    return Results.Created($"/notifications/{notification.Id}", notification);
});
app.MapPut("/notifications/{id:guid}/read", async (Guid id, NotificationsDb db) =>
{
    var notification = await db.Notifications.FindAsync(id);
    if (notification is null) return Results.NotFound();
    notification.Read = true;
    await db.SaveChangesAsync();
    return Results.Ok(notification);
});

app.Run();

record CreateNotificationRequest(Guid UserId, string Channel, string Message);
class Notification(Guid id, Guid userId, string channel, string message, bool read, DateTimeOffset createdAt)
{
    public Guid Id { get; set; } = id;
    public Guid UserId { get; set; } = userId;
    public string Channel { get; set; } = channel;
    public string Message { get; set; } = message;
    public bool Read { get; set; } = read;
    public DateTimeOffset CreatedAt { get; set; } = createdAt;
}
class NotificationsDb(DbContextOptions<NotificationsDb> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Notification>().HasKey(n => n.Id);
}
