namespace CertificateService.Api.Infrastructure.Security;

public interface IAuditLogger
{
    void Log(string action, Guid tenantId, string actor, object details);
}

public sealed class AuditLogger(ILogger<AuditLogger> logger) : IAuditLogger
{
    public void Log(string action, Guid tenantId, string actor, object details) =>
        logger.LogInformation("AUDIT {Action} Tenant={TenantId} Actor={Actor} Details={@Details}", action, tenantId, actor, details);
}
