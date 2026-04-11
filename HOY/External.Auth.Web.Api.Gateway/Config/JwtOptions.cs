namespace External.Auth.Web.Api.Gateway.Config;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Authority { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; } = true;
}
