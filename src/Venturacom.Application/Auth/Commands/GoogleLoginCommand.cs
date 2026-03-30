using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Auth;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Auth.DTOs;
using Venturacom.Application.Auth.Security;
using Venturacom.Domain.Entities;

namespace Venturacom.Application.Auth.Commands;

public sealed record GoogleLoginCommand(Guid TenantId, string IdToken) : IRequest<AuthTokensDto>;

public sealed class GoogleLoginCommandHandler(
    IApplicationDbContext dbContext,
    IGoogleTokenVerifier verifier,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<GoogleLoginCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var payload = await verifier.VerifyAsync(request.IdToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid Google token.");

        var normalizedEmail = payload.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                TenantId = request.TenantId,
                Email = payload.Email,
                NormalizedEmail = normalizedEmail,
                FullName = payload.FullName,
                PasswordHash = "GOOGLE_EXTERNAL",
                IsActive = true,
                LastLoginAtUtc = DateTime.UtcNow
            };
            await dbContext.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
        }

        var refreshTokenRaw = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
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
