using System.Security.Claims;
using Temple.Application.Identity;

namespace Temple.Api.Middleware;

public class CapabilityHashValidationMiddleware
{
    private readonly RequestDelegate _next;
    public CapabilityHashValidationMiddleware(RequestDelegate next) => _next = next;

    // Removed direct DI of CancellationToken; use ctx.RequestAborted
    public async Task Invoke(HttpContext ctx, TenantContext tenantCtx, ICapabilityHashProvider provider, ILogger<CapabilityHashValidationMiddleware> logger)
    {
        var ct = ctx.RequestAborted;
        if (tenantCtx.TenantId != null && ctx.User.Identity?.IsAuthenticated == true)
        {
            var claimed = ctx.User.Claims.FirstOrDefault(c => c.Type == "cap_hash")?.Value;
            var valid = await provider.ValidateAsync(tenantCtx.TenantId.Value, claimed, ct);
            if (!valid)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = new { code = "CAP_HASH_INVALID", message = "Re-authentication required" } }, ct);
                return;
            }
        }
        await _next(ctx);
    }
}

public static class CapabilityHashValidationExtensions
{
    public static IApplicationBuilder UseCapabilityHashValidation(this IApplicationBuilder app) => app.UseMiddleware<CapabilityHashValidationMiddleware>();
}
