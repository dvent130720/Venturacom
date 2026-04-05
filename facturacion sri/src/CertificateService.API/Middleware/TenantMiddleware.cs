namespace CertificateService.API.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var tenantHeader = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(tenantHeader) && Guid.TryParse(tenantHeader, out var tenantId))
        {
            context.Items["TenantId"] = tenantId;
        }

        await next(context);
    }
}
