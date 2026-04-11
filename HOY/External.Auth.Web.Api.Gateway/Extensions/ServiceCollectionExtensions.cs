using External.Auth.Web.Api.Gateway.Config;
using External.Auth.Web.Api.Gateway.Policies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

namespace External.Auth.Web.Api.Gateway.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<OpenTelemetryOptions>()
            .Bind(configuration.GetSection(OpenTelemetryOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<CircuitBreakerOptions>()
            .Bind(configuration.GetSection(CircuitBreakerOptions.SectionName))
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = jwtOptions.Authority;
                options.Audience = jwtOptions.Audience;
                options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                options.TokenValidationParameters.ValidAudience = jwtOptions.Audience;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ValidateLifetime = true;
            });

        return services;
    }

    public static IServiceCollection AddGatewayAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, TenantAccessHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.GatewayAuthenticated, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAccessRequirement());
            });

            options.AddPolicy(PolicyNames.TenantAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAccessRequirement());
            });
        });

        return services;
    }

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("gateway-fixed", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 120,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        return services;
    }

    public static IServiceCollection AddGatewayObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var telemetry = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        if (telemetry.Enabled)
        {
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(telemetry.ServiceName))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();
                });
        }

        return services;
    }

    public static IServiceCollection AddGatewayResilience(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("gateway-proxy");
        return services;
    }

    public static IServiceCollection AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(transformBuilderContext =>
            {
                transformBuilderContext.AddRequestTransform(transformContext =>
                {
                    if (transformContext.HttpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
                    {
                        transformContext.ProxyRequest.Headers.Remove("X-Tenant-Id");
                        transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-Tenant-Id", (IEnumerable<string>)tenantId);
                    }

                    var correlationId = transformContext.HttpContext.TraceIdentifier;
                    transformContext.ProxyRequest.Headers.Remove("X-Correlation-Id");
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

                    return ValueTask.CompletedTask;
                });

                transformBuilderContext.AddResponseTransform(transformContext =>
                {
                    transformContext.HttpContext.Response.Headers["X-Correlation-Id"] = transformContext.HttpContext.TraceIdentifier;
                    return ValueTask.CompletedTask;
                });
            });

        return services;
    }
}
