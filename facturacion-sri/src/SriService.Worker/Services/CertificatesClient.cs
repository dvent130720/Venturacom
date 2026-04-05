using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.Contracts;

namespace SriService.Worker.Services;

public sealed class CertificatesClient(HttpClient httpClient, IConfiguration configuration) : ICertificatesClient
{
    public async Task<InternalCertificateResponse> GetActiveCertificateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var apiKey = configuration["CertificateService:InternalApiKey"] ?? throw new InvalidOperationException("missing internal key");
        httpClient.DefaultRequestHeaders.Remove("X-Internal-ApiKey");
        httpClient.DefaultRequestHeaders.Add("X-Internal-ApiKey", apiKey);
        var response = await httpClient.GetFromJsonAsync<Response>($"certificates/internal/{tenantId}/raw", cancellationToken)
            ?? throw new InvalidOperationException("No certificate returned");

        return new InternalCertificateResponse(Convert.FromBase64String(response.P12Base64), response.Password, response.ExpirationDateUtc);
    }

    private sealed record Response(string P12Base64, string Password, DateTime ExpirationDateUtc);
}
