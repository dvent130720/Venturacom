namespace Venturacom.Application.Auth.DTOs;

public sealed record AuthTokensDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
