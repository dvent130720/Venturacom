namespace IdentityService.Api.Contracts;

public sealed record LoginRequest(string TenantId, string Username, string Password);
