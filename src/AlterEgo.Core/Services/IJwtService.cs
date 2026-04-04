using AlterEgo.Core.Entities;

namespace AlterEgo.Core.Services;

public interface IJwtService
{
    string GenerateAccessToken(UserEntity user);
    string GenerateRefreshToken();
    DateTimeOffset GetRefreshTokenExpiration();
}
