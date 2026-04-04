namespace SRI.Facturacion.Infrastructure.Configuration;

public sealed class SriEndpointsOptions
{
    public const string SectionName = "SriEndpoints";
    public SriEnvironmentEndpoints Pruebas { get; set; } = new();
    public SriEnvironmentEndpoints Produccion { get; set; } = new();
}

public sealed class SriEnvironmentEndpoints
{
    public string Recepcion { get; set; } = string.Empty;
    public string Autorizacion { get; set; } = string.Empty;
}
