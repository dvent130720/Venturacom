using System.Threading.Channels;
using Venturacom.Application.Abstractions.Queue;

namespace Venturacom.Infrastructure.Queue;

public sealed class InMemoryInvoiceQueue : IInvoiceQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public Task PublishAsync(Guid invoiceJobId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(invoiceJobId, cancellationToken).AsTask();

    public async Task<Guid?> ConsumeAsync(CancellationToken cancellationToken = default)
    {
        var canRead = await _channel.Reader.WaitToReadAsync(cancellationToken);
        if (!canRead) return null;
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
