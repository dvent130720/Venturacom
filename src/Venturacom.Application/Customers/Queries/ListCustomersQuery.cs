using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Customers.DTOs;

namespace Venturacom.Application.Customers.Queries;

public sealed record ListCustomersQuery : IRequest<IReadOnlyList<CustomerDto>>;

public sealed class ListCustomersQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<ListCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public async Task<IReadOnlyList<CustomerDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .Select(x => new CustomerDto(x.Id, x.Name, x.Identification, x.Email, x.Address))
            .ToListAsync(cancellationToken);
    }
}
