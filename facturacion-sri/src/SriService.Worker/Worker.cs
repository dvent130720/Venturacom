using Polly;
using Shared.Contracts;

namespace SriService.Worker;

public sealed class Worker(ILogger<Worker> logger, ICertificatesClient certificatesClient) : BackgroundService
{
    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new Polly.Retry.RetryStrategyOptions { MaxRetryAttempts = 3, Delay = TimeSpan.FromSeconds(2) })
        .Build();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SRI worker started. Waiting for FacturaCreada events...");

        // Replace polling mock with RabbitMQ consumer implementation.
        while (!stoppingToken.IsCancellationRequested)
        {
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            await _retry.ExecuteAsync(async token =>
            {
                var cert = await certificatesClient.GetActiveCertificateAsync(tenantId, token);
                logger.LogInformation("Certificate ready for signing. Expiration: {ExpirationDateUtc}", cert.ExpirationDateUtc);
                // Sign XML and continue SRI flow.
            }, stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
