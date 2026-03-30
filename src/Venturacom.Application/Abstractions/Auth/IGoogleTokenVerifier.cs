namespace Venturacom.Application.Abstractions.Auth;

public interface IGoogleTokenVerifier
{
    Task<(string Email, string FullName)?> VerifyAsync(string idToken, CancellationToken cancellationToken);
}
