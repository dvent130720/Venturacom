using System.Threading.RateLimiting;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Venturacom.API.Extensions;
using Venturacom.API.Middleware;
using Venturacom.Application;
using Venturacom.Infrastructure;
using Venturacom.Infrastructure.Caching;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(builder.Configuration["Loki:Url"] ?? "http://loki:3100")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddProblemDetails();

var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
builder.Services.AddHealthChecks().AddRedis(redisOptions.ToConnectionString(), name: "redis");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 500,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseTenantMiddleware();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
