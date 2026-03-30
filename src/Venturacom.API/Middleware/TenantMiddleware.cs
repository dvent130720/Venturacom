using System.Security.Claims;
using Venturacom.Application.Common;

namespace Venturacom.API.Middleware;

public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ITenantContext tenantContext)
    {
        var tenantClaim = context.User.FindFirstValue("tenant_id")
            ?? context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (!Guid.TryParse(tenantClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is required." });
            return;
        }

        tenantContext.SetTenantId(tenantId);
        await next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder app) => app.UseMiddleware<TenantMiddleware>();
}
