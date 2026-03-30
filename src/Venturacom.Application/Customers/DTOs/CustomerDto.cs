namespace Venturacom.Application.Customers.DTOs;

public sealed record CustomerDto(Guid Id, string Name, string Identification, string Email, string Address);
