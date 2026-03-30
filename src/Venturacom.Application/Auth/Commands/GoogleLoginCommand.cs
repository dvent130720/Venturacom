using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Auth;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Auth.DTOs;
using Venturacom.Application.Auth.Security;
using Venturacom.Domain.Entities;
using Venturacom.Domain.Entities.Security;

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

            var ownerRole = await EnsureOwnerRoleAsync(request.TenantId, cancellationToken);
            await dbContext.UserRoles.AddAsync(new UserRole
            {
                TenantId = request.TenantId,
                User = user,
                RoleId = ownerRole.Id
            }, cancellationToken);
        }
        else
        {
            user.LastLoginAtUtc = DateTime.UtcNow;
        }

        var roles = await dbContext.UserRoles.AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Join(dbContext.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);
        if (roles.Count == 0)
        {
            roles.Add("Owner");
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
        return new AuthTokensDto(tokenGenerator.Generate(user, roles), refreshTokenRaw, DateTime.UtcNow.AddDays(15));
    }

    private async Task<Role> EnsureOwnerRoleAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "Owner", cancellationToken);
        if (role is not null)
        {
            return role;
        }

        role = new Role { TenantId = tenantId, Name = "Owner" };
        await dbContext.Roles.AddAsync(role, cancellationToken);
        return role;
    }
}
