using CertificateService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CertificateService.Infrastructure.Services;

public class ExpiracionCertificadosWorker(IServiceScopeFactory scopeFactory, ILogger<ExpiracionCertificadosWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CertificateDbContext>();
            var porExpirar = await db.Certificados
                .Where(c => c.FechaExpiracion <= DateTimeOffset.UtcNow.AddDays(15) && c.EstaActivo)
                .Select(c => new { c.Id, c.TenantId, c.FechaExpiracion })
                .ToListAsync(stoppingToken);

            foreach (var item in porExpirar)
            {
                logger.LogWarning("Certificado por expirar. tenant={TenantId}, certificado={CertificadoId}, fecha={FechaExpiracion}", item.TenantId, item.Id, item.FechaExpiracion);
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
