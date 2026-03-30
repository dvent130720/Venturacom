using Venturacom.Domain.Enums;
using Venturacom.Infrastructure.Caching;

namespace Venturacom.Tests;

public sealed class SecurityAndRedisTests
{
    [Fact]
    public void ModuleType_ShouldContainExpectedModules()
    {
        var modules = Enum.GetValues<ModuleType>();
        Assert.Contains(ModuleType.Facturacion, modules);
        Assert.Contains(ModuleType.Inventario, modules);
        Assert.Contains(ModuleType.Reportes, modules);
        Assert.Contains(ModuleType.ModuloIA, modules);
    }

    [Fact]
    public void RedisOptions_ShouldBuildConnectionStringFromConfig()
    {
        var options = new RedisOptions
        {
            Server = "redis.internal",
            Port = 6380,
            Password = "secret",
            Database = 2,
            Ssl = true
        };

        var connection = options.ToConnectionString();
        Assert.Contains("redis.internal:6380", connection);
        Assert.Contains("password=secret", connection);
        Assert.Contains("defaultDatabase=2", connection);
        Assert.Contains("ssl=true", connection);
    }
}
