using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CertificateService.Api.Infrastructure.Security;

public sealed class InternalServiceAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = configuration["InternalAuth:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected)) return Task.FromResult(AuthenticateResult.Fail("Missing InternalAuth:ApiKey config"));
        if (!Request.Headers.TryGetValue("X-Internal-ApiKey", out var current) || current != expected)
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var claims = new[] { new Claim(ClaimTypes.Name, "internal-service"), new Claim("scope", "certificate:raw") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
