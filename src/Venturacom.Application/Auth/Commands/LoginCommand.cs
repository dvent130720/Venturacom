using MediatR;
using Microsoft.EntityFrameworkCore;
using Venturacom.Application.Abstractions.Auth;
using Venturacom.Application.Abstractions.Persistence;
using Venturacom.Application.Auth.DTOs;
using Venturacom.Domain.Entities;

namespace Venturacom.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthTokensDto>;

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IRequestHandler<LoginCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email && x.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var refreshTokenRaw = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var refreshToken = new RefreshToken
        {
            TenantId = user.TenantId,
            UserId = user.Id,
            Token = refreshTokenRaw,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(15)
        };

        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(tokenGenerator.Generate(user), refreshToken.Token, refreshToken.ExpiresAtUtc);
    }
}
