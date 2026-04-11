namespace External.Auth.Web.Api.Gateway.Config;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; init; }

    public string ServiceName { get; init; } = "External.Auth.Web.Api.Gateway";
}
