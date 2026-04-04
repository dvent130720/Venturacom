using MediatR;
using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Domain.Entities;
using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Application.UseCases;
public sealed class CreateComprobanteHandler : IRequestHandler<CreateComprobanteCommand, Guid> {
  private readonly IComprobanteRepository _repo; private readonly IClaveAccesoGenerator _clave; private readonly IXmlBuilderFactory _builderFactory;
  public CreateComprobanteHandler(IComprobanteRepository repo, IClaveAccesoGenerator clave, IXmlBuilderFactory builderFactory){_repo=repo;_clave=clave;_builderFactory=builderFactory;}
  public async Task<Guid> Handle(CreateComprobanteCommand request, CancellationToken ct){
    var codigoNumerico = Random.Shared.Next(10000000,99999999).ToString();
    var claveAcceso = _clave.Generate(DateOnly.FromDateTime(DateTime.UtcNow), request.Payload.Tipo, request.Payload.Ruc, request.Payload.Ambiente, request.Payload.Serie, request.Payload.Secuencial, codigoNumerico, "1");
    var builder = _builderFactory.Resolve(request.Payload.Tipo);
    var xml = builder.BuildXml(request.Payload with { });
    if(!builder.Validate(xml, out var errors)) throw new InvalidOperationException(string.Join("|", errors));
    var entity = new Comprobante{ TenantId=request.Payload.TenantId, Tipo=request.Payload.Tipo, ClaveAcceso=claveAcceso, Serie=request.Payload.Serie, Secuencial=request.Payload.Secuencial, XmlGenerado=xml, Estado=EstadoComprobanteEnum.Generado, NextAttemptAtUtc=DateTime.UtcNow};
    entity.Detalles = request.Payload.Detalles.Select(x=> new DetalleComprobante{CodigoPrincipal=x.CodigoPrincipal,Descripcion=x.Descripcion,Cantidad=x.Cantidad,PrecioUnitario=x.PrecioUnitario,Descuento=x.Descuento,PrecioTotalSinImpuesto=x.PrecioTotalSinImpuesto}).ToList();
    entity.Impuestos = request.Payload.Impuestos.Select(x=> new Impuesto{Codigo=x.Codigo,CodigoPorcentaje=x.CodigoPorcentaje,BaseImponible=x.BaseImponible,Tarifa=x.Tarifa,Valor=x.Valor}).ToList();
    await _repo.AddAsync(entity, ct); return entity.Id;
  }
}
