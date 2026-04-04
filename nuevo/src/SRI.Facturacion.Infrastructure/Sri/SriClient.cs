using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SRI.Facturacion.Application.Contracts;
using SRI.Facturacion.Infrastructure.Configuration;

namespace SRI.Facturacion.Infrastructure.Sri;

public sealed class SriClient : ISriClient
{
    private readonly IHttpClientFactory _factory;
    private readonly SriEndpointsOptions _endpoints;

    public SriClient(IHttpClientFactory factory, IOptions<SriEndpointsOptions> endpoints)
    {
        _factory = factory;
        _endpoints = endpoints.Value;
    }

    public async Task<SriReceptionResult> EnviarRecepcionAsync(string signedXml, bool produccion, CancellationToken ct)
    {
        var client = _factory.CreateClient("SRI");
        var endpoint = produccion ? _endpoints.Produccion.Recepcion : _endpoints.Pruebas.Recepcion;
        var response = await client.PostAsJsonAsync(endpoint, new { xml = signedXml }, ct);

        if (!response.IsSuccessStatusCode)
        {
            return new SriReceptionResult("DEVUELTA", "HTTP", response.ReasonPhrase);
        }

        return new SriReceptionResult("RECIBIDA", null, null);
    }

    public async Task<SriAuthorizationResult> ConsultarAutorizacionAsync(string claveAcceso, bool produccion, CancellationToken ct)
    {
        var client = _factory.CreateClient("SRI");
        var endpoint = produccion ? _endpoints.Produccion.Autorizacion : _endpoints.Pruebas.Autorizacion;
        var response = await client.PostAsJsonAsync(endpoint, new { claveAcceso }, ct);

        if (!response.IsSuccessStatusCode)
        {
            return new SriAuthorizationResult("NO AUTORIZADO", null, null, null, response.ReasonPhrase);
        }

        return new SriAuthorizationResult("AUTORIZADO", Guid.NewGuid().ToString("N"), DateTime.UtcNow, "1", null);
    }
}
