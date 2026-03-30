using MediatR;
using Venturacom.Application.Abstractions.Caching;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Common;
using Venturacom.Application.Products.DTOs;
using Venturacom.Domain.Entities;

namespace Venturacom.Application.Products.Commands;

public sealed record CreateProductCommand(string Name, string Sku, decimal UnitPrice, decimal TaxRate) : IRequest<ProductDto>;

public sealed class CreateProductCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ICacheService cacheService) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = new Product
        {
            TenantId = tenantContext.TenantId,
            Name = request.Name,
            Sku = request.Sku,
            UnitPrice = request.UnitPrice,
            TaxRate = request.TaxRate
        };

        await dbContext.Products.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"tenant:{tenantContext.TenantId}:products:list", cancellationToken);

        return new ProductDto(entity.Id, entity.Name, entity.Sku, entity.UnitPrice, entity.TaxRate);
    }
}
