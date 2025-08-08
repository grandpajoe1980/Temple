using Temple.Application.Terminology;
using Temple.Api.Middleware;

namespace Temple.Api.Middleware;

public class TerminologyMiddleware
{
    private readonly RequestDelegate _next;
    public TerminologyMiddleware(RequestDelegate next) => _next = next;

    // Removed explicit CancellationToken parameter (was not resolvable via DI) and now use ctx.RequestAborted
    public async Task Invoke(HttpContext ctx, ITerminologyService terms, TenantContext tenantCtx, ILogger<TerminologyMiddleware> logger, IConfiguration config, Temple.Infrastructure.Persistence.AppDbContext db)
    {
        if (tenantCtx.TenantId != null)
        {
            var ct = ctx.RequestAborted;
            var tenant = await db.Tenants.FindAsync(new object?[] { tenantCtx.TenantId.Value }, ct);
            var resolved = await terms.GetResolvedAsync(tenantCtx.TenantId.Value, tenant?.TaxonomyId, ct);
            ctx.Items["terminology"] = resolved; // simple injection
        }
        await _next(ctx);
    }
}

public static class TerminologyMiddlewareExtensions
{
    public static IApplicationBuilder UseTerminology(this IApplicationBuilder app) => app.UseMiddleware<TerminologyMiddleware>();
}
