namespace SRI.Facturacion.Application.Contracts;
public interface ISriClient { Task<SriReceptionResult> EnviarRecepcionAsync(string signedXml, bool produccion, CancellationToken ct); Task<SriAuthorizationResult> ConsultarAutorizacionAsync(string claveAcceso, bool produccion, CancellationToken ct); }
public sealed record SriReceptionResult(string Estado, string? Codigo, string? Mensaje);
public sealed record SriAuthorizationResult(string Estado, string? NumeroAutorizacion, DateTime? FechaAutorizacion, string? Ambiente, string? Mensaje);
