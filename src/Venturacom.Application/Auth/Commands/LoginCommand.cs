using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Auth;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Auth.DTOs;
using Venturacom.Application.Auth.Security;

namespace Venturacom.Application.Auth.Commands;

public sealed record LoginCommand(Guid TenantId, string Email, string Password) : IRequest<AuthTokensDto>;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<LoginCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.NormalizedEmail == normalizedEmail && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        var roles = await dbContext.UserRoles.AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Join(dbContext.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
            .ToListAsync(cancellationToken);

        var refreshTokenRaw = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        await dbContext.RefreshTokens.AddAsync(new()
        {
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = RefreshTokenHasher.Hash(refreshTokenRaw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(15)
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AuthTokensDto(tokenGenerator.Generate(user, roles), refreshTokenRaw, DateTime.UtcNow.AddDays(15));
    }
}
