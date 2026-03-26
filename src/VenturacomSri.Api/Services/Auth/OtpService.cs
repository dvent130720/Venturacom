using StackExchange.Redis;

namespace VenturacomSri.Api.Services.Auth;

/// <summary>
/// Genera, guarda y valida códigos OTP de 6 dígitos usando Redis.
/// TTL por defecto: 10 minutos.
/// </summary>
public class OtpService
{
    private readonly IDatabase _redis;
    private const int TtlMinutes = 10;
    private const string Prefix = "otp:";

    public OtpService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    /// <summary>Genera un código OTP de 6 dígitos y lo persiste en Redis.</summary>
    public async Task<string> GenerateAsync(string email)
    {
        var code = Random.Shared.Next(100_000, 999_999).ToString();
        var key  = Prefix + email.ToLowerInvariant();
        await _redis.StringSetAsync(key, code, TimeSpan.FromMinutes(TtlMinutes));
        return code;
    }

    /// <summary>Valida el código y lo elimina si es correcto (uso único).</summary>
    public async Task<bool> ValidateAsync(string email, string code)
    {
        var key   = Prefix + email.ToLowerInvariant();
        var stored = await _redis.StringGetAsync(key);
        if (stored.IsNullOrEmpty || stored != code) return false;

        await _redis.KeyDeleteAsync(key);
        return true;
    }
}
