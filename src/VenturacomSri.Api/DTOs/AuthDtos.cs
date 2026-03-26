using System.ComponentModel.DataAnnotations;

namespace VenturacomSri.Api.DTOs;

// ── Requests ────────────────────────────────────────────────────────────────

public record SendOtpRequest(
    [Required, EmailAddress, MaxLength(256)] string Email);

public record VerifyOtpRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, Length(6, 6)] string Code);

public record GoogleLoginRequest(
    [Required] string IdToken);

public record RefreshRequest(
    [Required] string RefreshToken);

// ── Responses ───────────────────────────────────────────────────────────────

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User,
    bool IsNewUser);

public record UserDto(
    Guid Id,
    string Email,
    string? Name,
    string? AvatarUrl,
    string Provider);
