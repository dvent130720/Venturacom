using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Domain.Common;

namespace Venturacom.Infrastructure.Persistence;

public sealed class Repository<T>(ApplicationDbContext context) : IRepository<T> where T : BaseEntity
{
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Set<T>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await context.Set<T>().Where(predicate).ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await context.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => context.Set<T>().Update(entity);

    public void Remove(T entity) => context.Set<T>().Remove(entity);
}
