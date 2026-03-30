using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Venturacom.Domain.Entities.Security;
using Venturacom.Domain.Enums;
using Venturacom.Infrastructure.Persistence;

namespace Venturacom.Infrastructure.Services;

public sealed class SecuritySeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await db.Permissions.AsNoTracking().ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            return;
        }

        var actions = new[] { "Read", "Write", "Approve", "Admin" };
        var permissions = Enum.GetValues<ModuleType>()
            .SelectMany(module => actions.Select(action => new Permission
            {
                Module = module,
                Action = action,
                IsActive = true
            }))
            .ToList();

        await db.Permissions.AddRangeAsync(permissions, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
