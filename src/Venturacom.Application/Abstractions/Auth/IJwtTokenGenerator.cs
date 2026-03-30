using Venturacom.Domain.Entities;

namespace Venturacom.Application.Abstractions.Auth;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
