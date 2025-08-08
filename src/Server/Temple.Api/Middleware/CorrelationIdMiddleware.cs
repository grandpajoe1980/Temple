using System.Diagnostics;

namespace Temple.Api.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var correlationId = ctx.Request.Headers.TryGetValue(HeaderName, out var existing)
            ? existing.ToString()
            : Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
        ctx.Items[HeaderName] = correlationId;
        ctx.Response.Headers[HeaderName] = correlationId;
        await _next(ctx);
    }
}

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) => app.UseMiddleware<CorrelationIdMiddleware>();
}
