using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Auth;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Auth.DTOs;
using Venturacom.Application.Auth.Security;
using Venturacom.Domain.Entities;

namespace Venturacom.Application.Auth.Commands;

public sealed record RegisterCommand(Guid TenantId, string Email, string Password, string FullName) : IRequest<AuthTokensDto>;

public sealed class RegisterCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<RegisterCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.TenantId == request.TenantId && x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var user = new User
        {
            TenantId = request.TenantId,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = request.FullName,
            LastLoginAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var refreshTokenRaw = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.RefreshTokens.AddAsync(new RefreshToken
        {
            TenantId = request.TenantId,
            User = user,
            Token = RefreshTokenHasher.Hash(refreshTokenRaw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(15)
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthTokensDto(tokenGenerator.Generate(user), refreshTokenRaw, DateTime.UtcNow.AddDays(15));
    }
}
