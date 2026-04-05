namespace Shared.Contracts;

public interface ICertificatesClient
{
    Task<InternalCertificateResponse> GetActiveCertificateAsync(Guid tenantId, CancellationToken cancellationToken);
}

public sealed record InternalCertificateResponse(byte[] P12Bytes, string Password, DateTime ExpirationDateUtc);
