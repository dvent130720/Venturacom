using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Infrastructure.Persistence;
using SRI.Facturacion.Infrastructure.Security;
using SRI.Facturacion.Infrastructure.Sri;
namespace SRI.Facturacion.Infrastructure;
public static class DependencyInjection {
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg){
    services.AddDbContext<AppDbContext>(o=>o.UseNpgsql(cfg.GetConnectionString("Default")));
    services.AddScoped<IComprobanteRepository,ComprobanteRepository>(); services.AddSingleton<IClaveAccesoGenerator,ClaveAccesoGenerator>(); services.AddSingleton<ICertificateProtector,AesCertificateProtector>(); services.AddSingleton<IXadesSigner,XadesBesSigner>();
    services.AddScoped<FacturaXmlBuilder>(); services.AddScoped<NotaCreditoXmlBuilder>(); services.AddScoped<NotaDebitoXmlBuilder>(); services.AddScoped<GuiaRemisionXmlBuilder>(); services.AddScoped<RetencionXmlBuilder>(); services.AddScoped<IXmlBuilderFactory,ComprobanteXmlBuilderFactory>();
    services.AddHttpClient("SRI").AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, i=>TimeSpan.FromSeconds(i*2)));
    services.AddScoped<ISriClient,SriClient>();
    return services;
  }
}
