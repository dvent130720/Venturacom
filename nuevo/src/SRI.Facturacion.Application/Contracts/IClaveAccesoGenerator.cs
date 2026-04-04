using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Application.Contracts;
public interface IClaveAccesoGenerator { string Generate(DateOnly fechaEmision, TipoComprobante tipo, string ruc, string ambiente, string serie, string secuencial, string codigoNumerico, string tipoEmision); }
