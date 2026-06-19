using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<UsersDb>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("UsersDb") ?? "Data Source=users.db"));

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "dev-secret-change-me-dev-secret-change-me";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsersDb>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "fcg-users-api" }));

app.MapPost("/users/register", async (RegisterUserRequest request, UsersDb db) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(email) || request.Password.Length < 6)
        return Results.BadRequest(new { message = "Informe nome, e-mail e senha com pelo menos 6 caracteres." });

    if (await db.Users.AnyAsync(u => u.Email == email))
        return Results.Conflict(new { message = "E-mail já cadastrado." });

    var user = new UserAccount(Guid.NewGuid(), request.Name.Trim(), email, HashPassword(request.Password), "Player", DateTimeOffset.UtcNow);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", new UserResponse(user.Id, user.Name, user.Email, user.Role, user.CreatedAt));
});

app.MapPost("/users/login", async (LoginRequest request, UsersDb db) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
    if (user is null || user.PasswordHash != HashPassword(request.Password))
        return Results.Unauthorized();

    var token = CreateToken(user, signingKey);
    return Results.Ok(new LoginResponse(token, new UserResponse(user.Id, user.Name, user.Email, user.Role, user.CreatedAt)));
});

app.MapGet("/users/me", (ClaimsPrincipal principal) =>
{
    var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = principal.FindFirstValue(ClaimTypes.Email);
    var name = principal.FindFirstValue(ClaimTypes.Name);
    var role = principal.FindFirstValue(ClaimTypes.Role);
    return Results.Ok(new { id, name, email, role });
}).RequireAuthorization();

app.Run();

static string HashPassword(string password)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes);
}

static string CreateToken(UserAccount user, SecurityKey key)
{
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };
    var token = new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials);
    return new JwtSecurityTokenHandler().WriteToken(token);
}

record RegisterUserRequest(string Name, string Email, string Password);
record LoginRequest(string Email, string Password);
record LoginResponse(string AccessToken, UserResponse User);
record UserResponse(Guid Id, string Name, string Email, string Role, DateTimeOffset CreatedAt);
record UserAccount(Guid Id, string Name, string Email, string PasswordHash, string Role, DateTimeOffset CreatedAt);

class UsersDb(DbContextOptions<UsersDb> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>().HasKey(u => u.Id);
        modelBuilder.Entity<UserAccount>().HasIndex(u => u.Email).IsUnique();
    }
}
