using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Products.DTOs;

namespace Venturacom.Application.Products.Queries;

public sealed record ListProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public sealed class ListProductsQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Products.AsNoTracking()
            .Select(x => new ProductDto(x.Id, x.Name, x.Sku, x.UnitPrice, x.TaxRate))
            .ToListAsync(cancellationToken);
    }
}
