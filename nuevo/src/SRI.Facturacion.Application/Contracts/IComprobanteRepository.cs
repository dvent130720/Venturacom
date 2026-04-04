using SRI.Facturacion.Domain.Entities;
namespace SRI.Facturacion.Application.Contracts;
public interface IComprobanteRepository { Task<Comprobante> AddAsync(Comprobante comprobante, CancellationToken ct); Task<Comprobante?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct); Task<IReadOnlyList<Comprobante>> GetPendingsAsync(int max, CancellationToken ct); Task UpdateAsync(Comprobante comprobante, CancellationToken ct); }
