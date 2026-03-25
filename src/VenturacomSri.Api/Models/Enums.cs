namespace VenturacomSri.Api.Models;

public enum InvoiceStatus
{
    Draft,       // Creada, sin firmar
    Signed,      // XML firmado con XAdES-BES
    Sent,        // Enviada a SRI, pendiente de respuesta
    Authorized,  // Autorizada por SRI
    Rejected,    // Rechazada por SRI
    Cancelled    // Anulada
}

public enum SriEnvironment
{
    Test = 1,
    Production = 2
}
