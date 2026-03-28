namespace VenturacomApi.Models;

public record SendOtpRequest(string Email);

public record VerifyOtpRequest(string Email, string Otp);

public record AuthUser(string Id, string Email, string? Name, string? PhotoUrl, string Provider);

public record AuthResponse(string Token, AuthUser User);
