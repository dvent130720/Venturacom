using CertificateService.Application.Interfaces;
using CertificateService.Application.UseCases;
using CertificateService.Domain.Interfaces;
using CertificateService.Infrastructure.Data;
using CertificateService.Infrastructure.Repositories;
using CertificateService.Infrastructure.Security;
using CertificateService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CertificateService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CertificateDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddStackExchangeRedisCache(opt =>
        {
            opt.Configuration = configuration.GetConnectionString("Redis");
            opt.InstanceName = "certsvc:";
        });

        services.AddScoped<ICertificadoRepository, CertificadoRepository>();
        services.AddScoped<IEncriptacionService, AesGcmEncriptacionService>();
        services.AddScoped<ICertificadoCriptoService, CertificadoCriptoService>();
        services.AddScoped<ICertificadosUseCases, CertificadosUseCases>();
        services.AddScoped<ICertificateSigningService>(sp => (CertificadosUseCases)sp.GetRequiredService<ICertificadosUseCases>());
        services.AddScoped<IFirmaXmlService, FirmaXmlService>();
        services.AddHostedService<ExpiracionCertificadosWorker>();

        return services;
    }
}
