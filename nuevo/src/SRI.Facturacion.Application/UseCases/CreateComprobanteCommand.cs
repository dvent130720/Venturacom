using MediatR;
using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Application.UseCases;
public sealed record CreateComprobanteCommand(ComprobantePayload Payload) : IRequest<Guid>;
