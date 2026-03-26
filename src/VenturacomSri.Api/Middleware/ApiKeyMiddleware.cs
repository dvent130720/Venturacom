namespace VenturacomSri.Api.Middleware;

/// <summary>
/// Middleware de autenticación por API Key.
/// Header requerido: X-API-Key: &lt;clave&gt;
/// El endpoint /health queda exento.
/// </summary>
public class ApiKeyMiddleware
{
    private const string HeaderName = "X-API-Key";
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        // Eximir el health check, dashboard Hangfire y rutas de autenticación
        var path = ctx.Request.Path.Value ?? "";
        if (path.StartsWith("/health",   StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var key))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                """{"success":false,"error":"Header X-API-Key requerido."}""");
            return;
        }

        var expected = ctx.RequestServices
            .GetRequiredService<IConfiguration>()["Api:Key"];

        if (key != expected)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                """{"success":false,"error":"API Key inválido."}""");
            return;
        }

        await _next(ctx);
    }
}
