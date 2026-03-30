using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Caching;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Common;
using Venturacom.Application.Products.DTOs;

namespace Venturacom.Application.Products.Queries;

public sealed record ListProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public sealed class ListProductsQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ITenantContext tenantContext) : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"tenant:{tenantContext.TenantId}:products:list";
        var cached = await cacheService.GetAsync<IReadOnlyList<ProductDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var products = await dbContext.Products.AsNoTracking()
            .Select(x => new ProductDto(x.Id, x.Name, x.Sku, x.UnitPrice, x.TaxRate))
            .ToListAsync(cancellationToken);

        await cacheService.SetAsync(cacheKey, products, TimeSpan.FromMinutes(3), cancellationToken);
        return products;
    }
}
