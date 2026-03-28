using System.Collections.Concurrent;

namespace VenturacomApi.Services;

public class OtpEntry
{
    public string Code { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public int Attempts { get; set; }
}

public interface IOtpService
{
    string GenerateAndStore(string email);
    bool Verify(string email, string code);
}

public class OtpService : IOtpService
{
    private static readonly ConcurrentDictionary<string, OtpEntry> _store = new();
    private const int ExpiryMinutes = 10;
    private const int MaxAttempts = 5;

    public string GenerateAndStore(string email)
    {
        var code = Random.Shared.Next(100_000, 999_999).ToString();
        var entry = new OtpEntry
        {
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpiryMinutes),
            Attempts = 0
        };
        _store[email.ToLower()] = entry;
        return code;
    }

    public bool Verify(string email, string code)
    {
        var key = email.ToLower();
        if (!_store.TryGetValue(key, out var entry)) return false;
        if (DateTime.UtcNow > entry.ExpiresAt) { _store.TryRemove(key, out _); return false; }
        if (entry.Attempts >= MaxAttempts) { _store.TryRemove(key, out _); return false; }

        entry.Attempts++;
        if (entry.Code != code) return false;

        _store.TryRemove(key, out _);
        return true;
    }
}
