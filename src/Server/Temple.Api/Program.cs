using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Temple.Infrastructure.Persistence;
using Temple.Application.Tenants;
using Temple.Infrastructure.Tenants;

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

        app.Run();
    }
}
