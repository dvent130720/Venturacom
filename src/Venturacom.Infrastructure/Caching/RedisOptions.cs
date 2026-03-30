namespace Venturacom.Infrastructure.Caching;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string Server { get; init; } = "redis";
    public int Port { get; init; } = 6379;
    public string? Password { get; init; }
    public int Database { get; init; }
    public bool Ssl { get; init; }

    public string ToConnectionString()
    {
        var auth = string.IsNullOrWhiteSpace(Password) ? string.Empty : $",password={Password}";
        var ssl = Ssl ? ",ssl=true" : string.Empty;
        return $"{Server}:{Port},abortConnect=false,defaultDatabase={Database}{auth}{ssl}";
    }
}
