using MediatR;
using Microsoft.AspNetCore.Mvc;
using SRI.Facturacion.Application;
using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Application.UseCases;
using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Api.Controllers;
[ApiController]
[Route("comprobantes")]
public sealed class ComprobantesController : ControllerBase {
  private readonly IMediator _mediator; private readonly IComprobanteRepository _repo;
  public ComprobantesController(IMediator mediator, IComprobanteRepository repo){_mediator=mediator;_repo=repo;}
  [HttpPost("factura")] public Task<Guid> PostFactura([FromBody] ComprobantePayload payload, CancellationToken ct)=>_mediator.Send(new CreateComprobanteCommand(payload with { Tipo=TipoComprobante.Factura }),ct);
  [HttpPost("nota-credito")] public Task<Guid> PostNotaCredito([FromBody] ComprobantePayload payload, CancellationToken ct)=>_mediator.Send(new CreateComprobanteCommand(payload with { Tipo=TipoComprobante.NotaCredito }),ct);
  [HttpPost("retencion")] public Task<Guid> PostRetencion([FromBody] ComprobantePayload payload, CancellationToken ct)=>_mediator.Send(new CreateComprobanteCommand(payload with { Tipo=TipoComprobante.Retencion }),ct);
  [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid tenantId, CancellationToken ct){ var c=await _repo.GetByIdAsync(id,tenantId,ct); return c is null?NotFound():Ok(c); }
  [HttpGet("{id:guid}/estado")] public async Task<IActionResult> GetEstado(Guid id, [FromQuery] Guid tenantId, CancellationToken ct){ var c=await _repo.GetByIdAsync(id,tenantId,ct); return c is null?NotFound():Ok(new { c.Estado, c.ErrorCode, c.ErrorMessage }); }
}
