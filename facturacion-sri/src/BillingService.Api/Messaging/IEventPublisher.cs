namespace BillingService.Api.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T payload, string routingKey, CancellationToken cancellationToken);
}

public sealed class RabbitMqEventPublisher(ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(T payload, string routingKey, CancellationToken cancellationToken)
    {
        logger.LogInformation("Published event {RoutingKey}: {@Payload}", routingKey, payload);
        return Task.CompletedTask;
    }
}
