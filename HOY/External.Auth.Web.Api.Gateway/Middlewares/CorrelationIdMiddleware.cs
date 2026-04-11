using Microsoft.Extensions.Primitives;

namespace External.Auth.Web.Api.Gateway.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out StringValues correlationId) || StringValues.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        var correlationValue = correlationId.ToString();
        context.TraceIdentifier = correlationValue;
        context.Items[HeaderName] = correlationValue;
        context.Response.Headers[HeaderName] = correlationValue;

        await next(context);
    }
}
