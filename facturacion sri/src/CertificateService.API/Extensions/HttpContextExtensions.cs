namespace CertificateService.API.Extensions;

public static class HttpContextExtensions
{
    public static Guid ObtenerTenantId(this HttpContext context)
    {
        if (context.Items.TryGetValue("TenantId", out var value) && value is Guid tenantId)
            return tenantId;

        throw new UnauthorizedAccessException("Tenant no encontrado en header X-Tenant-Id");
    }
}
