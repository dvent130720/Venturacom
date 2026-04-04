using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Extensions.Http;
using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Infrastructure.Configuration;
using SRI.Facturacion.Infrastructure.Persistence;
using SRI.Facturacion.Infrastructure.Security;
using SRI.Facturacion.Infrastructure.Sri;

namespace SRI.Facturacion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cfg.GetConnectionString("Default")));

        services.AddOptions<SriEndpointsOptions>()
            .Bind(cfg.GetSection(SriEndpointsOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Pruebas.Recepcion), "Pruebas.Recepcion is required")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Pruebas.Autorizacion), "Pruebas.Autorizacion is required")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Produccion.Recepcion), "Produccion.Recepcion is required")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Produccion.Autorizacion), "Produccion.Autorizacion is required")
            .ValidateOnStart();

        services.AddScoped<IComprobanteRepository, ComprobanteRepository>();
        services.AddSingleton<IClaveAccesoGenerator, ClaveAccesoGenerator>();
        services.AddSingleton<ICertificateProtector, AesCertificateProtector>();
        services.AddSingleton<IXadesSigner, XadesBesSigner>();

        services.AddScoped<FacturaXmlBuilder>();
        services.AddScoped<NotaCreditoXmlBuilder>();
        services.AddScoped<NotaDebitoXmlBuilder>();
        services.AddScoped<GuiaRemisionXmlBuilder>();
        services.AddScoped<RetencionXmlBuilder>();
        services.AddScoped<IXmlBuilderFactory, ComprobanteXmlBuilderFactory>();

        services.AddHttpClient("SRI")
            .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i * 2)));

        services.AddScoped<ISriClient, SriClient>();

        return services;
    }
}
