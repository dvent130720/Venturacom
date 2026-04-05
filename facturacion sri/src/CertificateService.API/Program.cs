using CertificateService.API.Middleware;
using CertificateService.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Servicio", "CertificateService")
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        ctx.Configuration["Loki:Url"] ?? "http://loki:3100",
        labels: new[] { new Serilog.Sinks.Grafana.Loki.LokiLabel { Key = "app", Value = "certificate-service" } }));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfraestructura(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = static (context, token) =>
    {
        context.HttpContext.Response.Headers.TryAdd("Retry-After", "60");
        return ValueTask.CompletedTask;
    };

    options.AddPolicy("limite-certificados", httpContext =>
    {
        var tenantId = httpContext.Items.TryGetValue("TenantId", out var tenant) ? tenant?.ToString() : "anonimo";

        return RateLimitPartition.GetFixedWindowLimiter(tenantId ?? "anonimo", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers().RequireRateLimiting("limite-certificados");

app.Run();
