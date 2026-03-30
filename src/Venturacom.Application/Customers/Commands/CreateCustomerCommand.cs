using MediatR;
using Venturacom.Application.Abstractions.Caching;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Common;
using Venturacom.Application.Customers.DTOs;
using Venturacom.Domain.Entities;

namespace Venturacom.Application.Customers.Commands;

public sealed record CreateCustomerCommand(string Name, string Identification, string Email, string Address) : IRequest<CustomerDto>;

public sealed class CreateCustomerCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    ICacheService cacheService) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = new Customer
        {
            TenantId = tenantContext.TenantId,
            Name = request.Name,
            Identification = request.Identification,
            Email = request.Email,
            Address = request.Address
        };

        await dbContext.Customers.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.RemoveAsync($"tenant:{tenantContext.TenantId}:customers:list", cancellationToken);

        return new CustomerDto(entity.Id, entity.Name, entity.Identification, entity.Email, entity.Address);
    }
}
