using External.Auth.Web.Api.Gateway.Middlewares;

namespace External.Auth.Web.Api.Gateway.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseGatewayPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<TenantMiddleware>();
        app.UseAuthorization();

        app.UseStatusCodePages(async statusCodeContext =>
        {
            var response = statusCodeContext.HttpContext.Response;
            if (response.HasStarted)
            {
                return;
            }

            await response.WriteAsJsonAsync(new
            {
                status = response.StatusCode,
                title = "Request was rejected",
                correlationId = statusCodeContext.HttpContext.TraceIdentifier
            });
        });

        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

        app.MapReverseProxy(proxyPipeline =>
        {
            proxyPipeline.Use(async (context, next) =>
            {
                await next();
            });
        }).RequireRateLimiting("gateway-fixed");

        return app;
    }
}
