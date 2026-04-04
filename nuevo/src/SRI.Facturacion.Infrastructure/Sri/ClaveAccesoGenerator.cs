using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Infrastructure.Sri;
public sealed class ClaveAccesoGenerator : IClaveAccesoGenerator {
  public string Generate(DateOnly fecha, TipoComprobante tipo, string ruc, string ambiente, string serie, string secuencial, string codigoNumerico, string tipoEmision){
    var baseKey=$"{fecha:ddMMyyyy}{(int)tipo:00}{ruc}{ambiente}{serie}{secuencial}{codigoNumerico}{tipoEmision}";
    var dv=Modulo11(baseKey); return baseKey+dv;
  }
  private static int Modulo11(string key){ int[] factors=[2,3,4,5,6,7]; int sum=0; int idx=0; for(int i=key.Length-1;i>=0;i--){ sum += (key[i]-'0')*factors[idx]; idx=(idx+1)%factors.Length;} var mod=11-(sum%11); return mod switch{11=>0,10=>1,_=>mod}; }
}
