using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Temple.Infrastructure.Persistence;
using Temple.Application.Tenants;
using Temple.Infrastructure.Tenants;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Temple.Domain.Users;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=temple;Username=postgres;Password=postgres"));

    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<PasswordHasher<User>>();

    builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt");

    builder.Services.AddSingleton<JwtSecurityTokenHandler>();

    builder.Services.AddHostedService<SeedStartupData>();

        builder.Services.AddHealthChecks();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapHealthChecks("/health");

        app.MapGet("/api/tenants/{id:guid}", async ([FromRoute] Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FindAsync(new object?[] { id }, ct);
            return tenant is null ? Results.NotFound() : Results.Ok(tenant);
        });

        app.MapPost("/api/tenants", async ([FromBody] TenantCreateRequest req, ITenantService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(req, ct);
            return Results.Created($"/api/tenants/{created.Id}", created);
        });

        app.MapPost("/api/auth/register", async (
            [FromBody] RegisterRequest request,
            AppDbContext db,
            PasswordHasher<User> hasher,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest();
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (existing != null) return Results.Conflict();
            // For now assign to first tenant (or create default) - simplified
            var tenant = await db.Tenants.OrderBy(t => t.CreatedUtc).FirstOrDefaultAsync(ct);
            if (tenant == null)
            {
                tenant = new Temple.Domain.Tenants.Tenant { Name = "Default Tenant", Slug = "default", Status = "active", CreatedUtc = DateTime.UtcNow };
                db.Tenants.Add(tenant);
            }
            var user = new User { Email = request.Email, TenantId = tenant.Id };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { id = user.Id });
        });

        app.MapPost("/api/auth/login", async (
            [FromBody] LoginRequest request,
            AppDbContext db,
            PasswordHasher<User> hasher,
            JwtSecurityTokenHandler tokenHandler,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (user == null) return Results.Unauthorized();
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed) return Results.Unauthorized();
            var jwtOpts = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Secret ?? "dev-secret-change"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("tid", user.TenantId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };
            var token = new JwtSecurityToken(
                issuer: jwtOpts.Issuer,
                audience: jwtOpts.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtOpts.ExpiryMinutes),
                signingCredentials: creds);
            var accessToken = tokenHandler.WriteToken(token);
            return Results.Ok(new { accessToken });
        });

        app.Run();
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);

public class JwtOptions
{
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? Secret { get; set; }
    public int ExpiryMinutes { get; set; } = 60;
}

public class SeedStartupData : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SeedStartupData> _logger;
    public SeedStartupData(IServiceProvider sp, ILogger<SeedStartupData> logger) { _sp = sp; _logger = logger; }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await db.Tenants.AnyAsync(cancellationToken))
        {
            var tenant = new Temple.Domain.Tenants.Tenant { Name = "Example Community", Slug = "example", Status = "active", CreatedUtc = DateTime.UtcNow };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default tenant {Tenant}", tenant.Id);
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
