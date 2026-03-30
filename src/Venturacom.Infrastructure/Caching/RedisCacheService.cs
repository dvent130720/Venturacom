using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Venturacom.Application.Abstractions.Caching;

namespace Venturacom.Infrastructure.Caching;

public sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, SerializerOptions);
        return cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);
}
