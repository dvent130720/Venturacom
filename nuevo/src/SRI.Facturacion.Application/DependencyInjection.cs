using Microsoft.Extensions.DependencyInjection;
using SRI.Facturacion.Application.UseCases;
namespace SRI.Facturacion.Application;
public static class DependencyInjection { public static IServiceCollection AddApplication(this IServiceCollection services){ services.AddMediatR(cfg=>cfg.RegisterServicesFromAssembly(typeof(CreateComprobanteHandler).Assembly)); return services; } }
