using System.Net.Http.Json;
using SRI.Facturacion.Application.Contracts;
namespace SRI.Facturacion.Infrastructure.Sri;
public sealed class SriClient : ISriClient {
  private readonly IHttpClientFactory _factory; public SriClient(IHttpClientFactory factory){_factory=factory;}
  public async Task<SriReceptionResult> EnviarRecepcionAsync(string signedXml, bool produccion, CancellationToken ct){ var client=_factory.CreateClient("SRI"); var endpoint=produccion?"https://srienlinea.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline":"https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline"; var response=await client.PostAsJsonAsync(endpoint,new{xml=signedXml},ct); if(!response.IsSuccessStatusCode) return new("DEVUELTA","HTTP",response.ReasonPhrase); return new("RECIBIDA",null,null); }
  public async Task<SriAuthorizationResult> ConsultarAutorizacionAsync(string claveAcceso, bool produccion, CancellationToken ct){ var client=_factory.CreateClient("SRI"); var endpoint=produccion?"https://srienlinea.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline":"https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline"; var response=await client.PostAsJsonAsync(endpoint,new{claveAcceso},ct); if(!response.IsSuccessStatusCode) return new("NO AUTORIZADO",null,null,null,response.ReasonPhrase); return new("AUTORIZADO",Guid.NewGuid().ToString("N"),DateTime.UtcNow,"1",null); }
}
