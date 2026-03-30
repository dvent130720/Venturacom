using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Venturacom.Application.Abstractions.Auth;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Abstractions.Queue;
using Venturacom.Application.Common;
using Venturacom.Infrastructure.Auth;
using Venturacom.Infrastructure.Persistence;
using Venturacom.Infrastructure.Queue;
using Venturacom.Infrastructure.Services;
using Venturacom.Infrastructure.Workers;

namespace Venturacom.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<ITenantContext, TenantContext>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IInvoiceQueue, InMemoryInvoiceQueue>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISriXmlService, SriXmlService>();
        services.AddHostedService<InvoiceWorker>();

        return services;
    }
}
