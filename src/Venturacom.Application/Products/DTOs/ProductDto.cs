namespace Venturacom.Application.Products.DTOs;

public sealed record ProductDto(Guid Id, string Name, string Sku, decimal UnitPrice, decimal TaxRate);
