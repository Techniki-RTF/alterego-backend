using AlterEgo.Core.Entities;

namespace AlterEgo.Core.Repositories;

public interface IUsersRepository
{
    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task AddAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
