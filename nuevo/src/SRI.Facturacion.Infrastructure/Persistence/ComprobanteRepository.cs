using Microsoft.EntityFrameworkCore;
using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Domain.Entities;
namespace SRI.Facturacion.Infrastructure.Persistence;
public sealed class ComprobanteRepository : IComprobanteRepository {
  private readonly AppDbContext _db; public ComprobanteRepository(AppDbContext db){_db=db;}
  public async Task<Comprobante> AddAsync(Comprobante c, CancellationToken ct){ _db.Comprobantes.Add(c); await _db.SaveChangesAsync(ct); return c; }
  public Task<Comprobante?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)=>_db.Comprobantes.Include(x=>x.Detalles).Include(x=>x.Impuestos).FirstOrDefaultAsync(x=>x.Id==id&&x.TenantId==tenantId,ct);
  public async Task<IReadOnlyList<Comprobante>> GetPendingsAsync(int max, CancellationToken ct)=> await _db.Comprobantes.Where(x=>x.NextAttemptAtUtc<=DateTime.UtcNow && x.Estado!=Domain.Enums.EstadoComprobanteEnum.Autorizado).OrderBy(x=>x.NextAttemptAtUtc).Take(max).ToListAsync(ct);
  public async Task UpdateAsync(Comprobante c, CancellationToken ct){ _db.Comprobantes.Update(c); await _db.SaveChangesAsync(ct); }
}
