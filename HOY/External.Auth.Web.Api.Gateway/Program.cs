using External.Auth.Web.Api.Gateway.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services
    .AddGatewayConfiguration(builder.Configuration)
    .AddGatewayAuthentication(builder.Configuration)
    .AddGatewayAuthorization()
    .AddGatewayRateLimiting()
    .AddGatewayObservability(builder.Configuration)
    .AddGatewayResilience(builder.Configuration)
    .AddGatewayReverseProxy(builder.Configuration);

var app = builder.Build();

app.UseGatewayPipeline();

app.Run();
