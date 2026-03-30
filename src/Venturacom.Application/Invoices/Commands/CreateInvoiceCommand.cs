using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Abstractions.Queue;
using Venturacom.Application.Common;
using Venturacom.Application.Invoices.DTOs;
using Venturacom.Domain.Entities;
using Venturacom.Domain.Enums;

namespace Venturacom.Application.Invoices.Commands;

public sealed record CreateInvoiceItemRequest(Guid ProductId, int Quantity);
public sealed record CreateInvoiceCommand(string Number, Guid CustomerId, IReadOnlyList<CreateInvoiceItemRequest> Items) : IRequest<InvoiceDto>;

public sealed class CreateInvoiceCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IInvoiceQueue queue) : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");

        var productIds = request.Items.Select(x => x.ProductId).ToHashSet();
        var products = await dbContext.Products.Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            throw new InvalidOperationException("Invalid products in invoice items.");
        }

        var invoice = new Invoice
        {
            TenantId = tenantContext.TenantId,
            CustomerId = customer.Id,
            Number = request.Number,
            Status = InvoiceStatus.Pending
        };

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var lineSubTotal = product.UnitPrice * item.Quantity;
            var lineTax = lineSubTotal * product.TaxRate;
            invoice.Items.Add(new InvoiceItem
            {
                TenantId = tenantContext.TenantId,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.UnitPrice,
                TaxRate = product.TaxRate,
                LineSubTotal = lineSubTotal,
                LineTax = lineTax,
                LineTotal = lineSubTotal + lineTax
            });
        }

        invoice.SubTotal = invoice.Items.Sum(x => x.LineSubTotal);
        invoice.TaxTotal = invoice.Items.Sum(x => x.LineTax);
        invoice.Total = invoice.SubTotal + invoice.TaxTotal;

        var job = new InvoiceJob
        {
            TenantId = tenantContext.TenantId,
            Invoice = invoice,
            Status = InvoiceJobStatus.Pending
        };

        await dbContext.Invoices.AddAsync(invoice, cancellationToken);
        await dbContext.InvoiceJobs.AddAsync(job, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await queue.PublishAsync(job.Id, cancellationToken);

        return new InvoiceDto(invoice.Id, invoice.Number, invoice.CustomerId, invoice.Status, invoice.Total,
            invoice.Items.Select(i => new InvoiceItemDto(i.ProductId, i.Quantity, i.UnitPrice, i.TaxRate, i.LineTotal)).ToList());
    }
}
