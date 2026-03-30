using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Invoices.DTOs;

namespace Venturacom.Application.Invoices.Queries;

public sealed record ListInvoicesQuery : IRequest<IReadOnlyList<InvoiceDto>>;

public sealed class ListInvoicesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<ListInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(ListInvoicesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Items)
            .Select(x => new InvoiceDto(x.Id, x.Number, x.CustomerId, x.Status, x.Total,
                x.Items.Select(i => new InvoiceItemDto(i.ProductId, i.Quantity, i.UnitPrice, i.TaxRate, i.LineTotal)).ToList()))
            .ToListAsync(cancellationToken);
    }
}
