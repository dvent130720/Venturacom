using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Caching;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Common;
using Venturacom.Application.Customers.DTOs;

namespace Venturacom.Application.Customers.Queries;

public sealed record ListCustomersQuery : IRequest<IReadOnlyList<CustomerDto>>;

public sealed class ListCustomersQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ITenantContext tenantContext) : IRequestHandler<ListCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public async Task<IReadOnlyList<CustomerDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"tenant:{tenantContext.TenantId}:customers:list";
        var cached = await cacheService.GetAsync<IReadOnlyList<CustomerDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Select(x => new CustomerDto(x.Id, x.Name, x.Identification, x.Email, x.Address))
            .ToListAsync(cancellationToken);

        await cacheService.SetAsync(cacheKey, customers, TimeSpan.FromMinutes(3), cancellationToken);
        return customers;
    }
}
