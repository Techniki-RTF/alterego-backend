using AlterEgo.Core.Entities;

namespace AlterEgo.Core.Repositories;

public interface IRefreshTokensRepository
{
    Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshTokenEntity>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
