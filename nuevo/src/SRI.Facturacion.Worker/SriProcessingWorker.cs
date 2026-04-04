using Microsoft.Extensions.Hosting;
using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Domain.Enums;
namespace SRI.Facturacion.Worker;
public sealed class SriProcessingWorker : BackgroundService {
  private readonly IServiceScopeFactory _scopeFactory; private readonly ILogger<SriProcessingWorker> _logger;
  public SriProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<SriProcessingWorker> logger){_scopeFactory=scopeFactory;_logger=logger;}
  protected override async Task ExecuteAsync(CancellationToken stoppingToken){
    while(!stoppingToken.IsCancellationRequested){
      using var scope=_scopeFactory.CreateScope();
      var repo=scope.ServiceProvider.GetRequiredService<IComprobanteRepository>(); var sri=scope.ServiceProvider.GetRequiredService<ISriClient>();
      var pendientes=await repo.GetPendingsAsync(25,stoppingToken);
      foreach(var c in pendientes){
        try{
          if(c.Estado==EstadoComprobanteEnum.Generado || c.Estado==EstadoComprobanteEnum.Firmado || c.Estado==EstadoComprobanteEnum.Enviado){
            var recep=await sri.EnviarRecepcionAsync(c.XmlFirmado ?? c.XmlGenerado!, false, stoppingToken);
            c.Estado=recep.Estado=="RECIBIDA"?EstadoComprobanteEnum.Recibida:EstadoComprobanteEnum.Devuelta; c.ErrorCode=recep.Codigo; c.ErrorMessage=recep.Mensaje;
          }
          if(c.Estado==EstadoComprobanteEnum.Recibida){ var auth=await sri.ConsultarAutorizacionAsync(c.ClaveAcceso,false,stoppingToken); c.Estado=auth.Estado=="AUTORIZADO"?EstadoComprobanteEnum.Autorizado:EstadoComprobanteEnum.NoAutorizado; c.ErrorMessage=auth.Mensaje; }
          c.Intentos++; c.NextAttemptAtUtc= c.Estado==EstadoComprobanteEnum.Autorizado?DateTime.UtcNow.AddYears(10):DateTime.UtcNow.AddMinutes(Math.Min(30,c.Intentos*2));
          await repo.UpdateAsync(c,stoppingToken);
        }catch(Exception ex){ c.Intentos++; c.ErrorMessage=ex.Message; c.NextAttemptAtUtc=DateTime.UtcNow.AddMinutes(5); await repo.UpdateAsync(c,stoppingToken); _logger.LogError(ex,"Error procesando {Id}",c.Id); }
      }
      await Task.Delay(TimeSpan.FromSeconds(10),stoppingToken);
    }
  }
}
