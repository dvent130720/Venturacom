using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

namespace External.Auth.Web.Api.Gateway.Middlewares;

public sealed class TenantMiddleware(RequestDelegate next)
{
    private static readonly PathString LoginPath = new("/v1/auth/login");
    private static readonly PathString HealthPath = new("/health");
    private const string HeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(LoginPath) || context.Request.Path.StartsWithSegments(HealthPath))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out StringValues tenantId) || StringValues.IsNullOrEmpty(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status400BadRequest,
                title = "Tenant header is required",
                detail = $"Header '{HeaderName}' is mandatory for {context.Request.GetDisplayUrl()}"
            });
            return;
        }

        context.Items[HeaderName] = tenantId.ToString();
        await next(context);
    }
}
