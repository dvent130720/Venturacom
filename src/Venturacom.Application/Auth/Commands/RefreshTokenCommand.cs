using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Auth;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Auth.DTOs;
using Venturacom.Application.Auth.Security;
using Venturacom.Domain.Entities;

namespace Venturacom.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokensDto>;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == RefreshTokenHasher.Hash(request.RefreshToken), cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (storedToken.IsExpired || storedToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        var replacementRaw = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        storedToken.ReplacedByToken = RefreshTokenHasher.Hash(replacementRaw);

        var replacement = new RefreshToken
        {
            TenantId = storedToken.TenantId,
            UserId = storedToken.UserId,
            Token = RefreshTokenHasher.Hash(replacementRaw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(15)
        };

        await dbContext.RefreshTokens.AddAsync(replacement, cancellationToken);
        var roles = await dbContext.UserRoles.AsNoTracking()
            .Where(x => x.UserId == storedToken.UserId)
            .Join(dbContext.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(tokenGenerator.Generate(storedToken.User, roles), replacementRaw, replacement.ExpiresAtUtc);
    }
}
