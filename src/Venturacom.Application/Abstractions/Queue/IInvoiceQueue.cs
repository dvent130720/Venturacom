namespace Venturacom.Application.Abstractions.Queue;

public interface IInvoiceQueue
{
    Task PublishAsync(Guid invoiceJobId, CancellationToken cancellationToken = default);
    Task<Guid?> ConsumeAsync(CancellationToken cancellationToken = default);
}
