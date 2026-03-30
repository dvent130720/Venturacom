using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Venturacom.Application.Abstractions.Auth;

namespace Venturacom.Infrastructure.Auth;

public sealed class GoogleTokenVerifier(IOptions<GoogleAuthOptions> options) : IGoogleTokenVerifier
{
    public async Task<(string Email, string FullName)?> VerifyAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [options.Value.ClientId]
            });

            return (payload.Email, payload.Name);
        }
        catch
        {
            return null;
        }
    }
}
