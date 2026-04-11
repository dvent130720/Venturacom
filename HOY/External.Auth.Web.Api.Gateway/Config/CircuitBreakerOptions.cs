namespace External.Auth.Web.Api.Gateway.Config;

public sealed class CircuitBreakerOptions
{
    public const string SectionName = "Resilience:CircuitBreaker";

    public bool Enabled { get; init; }

    public int FailureRatioPercentage { get; init; } = 50;

    public int MinimumThroughput { get; init; } = 20;

    public int SamplingDurationSeconds { get; init; } = 30;

    public int BreakDurationSeconds { get; init; } = 15;
}
