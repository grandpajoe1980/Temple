using Microsoft.EntityFrameworkCore;
using Temple.Infrastructure.Persistence;

namespace Temple.Api.Middleware;

public class TenantContext
{
    public Guid? TenantId { get; internal set; }
    public string? TenantSlug { get; internal set; }
}

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, TenantContext tenantCtx, AppDbContext db, ILogger<TenantResolutionMiddleware> logger)
    {
        // Skip for swagger & root
        if (!ctx.Request.Path.StartsWithSegments("/swagger"))
        {
            var host = ctx.Request.Host.Host;
            string? slug = null;
            if (host.Contains('.'))
            {
                var parts = host.Split('.');
                if (parts.Length > 2) // subdomain. root domain parts
                {
                    slug = parts[0];
                }
            }
            slug ??= ctx.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(slug))
            {
                var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
                if (tenant != null)
                {
                    tenantCtx.TenantId = tenant.Id;
                    tenantCtx.TenantSlug = tenant.Slug;
                }
                else
                {
                    logger.LogDebug("Tenant slug {Slug} not found", slug);
                }
            }
        }
        await _next(ctx);
    }
}

public static class TenantResolutionExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app) => app.UseMiddleware<TenantResolutionMiddleware>();
}
