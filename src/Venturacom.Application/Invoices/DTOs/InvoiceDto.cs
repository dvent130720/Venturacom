using Venturacom.Domain.Enums;

namespace Venturacom.Application.Invoices.DTOs;

public sealed record InvoiceItemDto(Guid ProductId, int Quantity, decimal UnitPrice, decimal TaxRate, decimal LineTotal);
public sealed record InvoiceDto(Guid Id, string Number, Guid CustomerId, InvoiceStatus Status, decimal Total, IReadOnlyList<InvoiceItemDto> Items);
