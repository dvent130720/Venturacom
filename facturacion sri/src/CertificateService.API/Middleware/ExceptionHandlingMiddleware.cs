using System.Net;
using System.Text.Json;

namespace CertificateService.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, codigo, mensaje) = Mapear(ex);
            logger.LogError(ex, "Error controlado codigo={Codigo} path={Path}", codigo, context.Request.Path);

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = codigo,
                mensaje,
                correlationId = context.TraceIdentifier
            }));
        }
    }

    private static (HttpStatusCode status, string codigo, string mensaje) Mapear(Exception ex) => ex switch
    {
        UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "tenant_no_autorizado", ex.Message),
        KeyNotFoundException => (HttpStatusCode.NotFound, "no_encontrado", ex.Message),
        InvalidOperationException => (HttpStatusCode.BadRequest, "validacion_negocio", ex.Message),
        _ => (HttpStatusCode.InternalServerError, "error_interno", "Ocurrió un error inesperado")
    };
}
