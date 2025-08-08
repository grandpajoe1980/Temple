using System.Net.Mime;
using System.Text.Json;

namespace Temple.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            var traceId = ctx.Items["X-Correlation-Id"]?.ToString() ?? ctx.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception {TraceId}", traceId);
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = MediaTypeNames.Application.Json;
            var problem = new
            {
                traceId,
                error = new { code = "UNHANDLED_ERROR", message = "An unexpected error occurred." }
            };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
