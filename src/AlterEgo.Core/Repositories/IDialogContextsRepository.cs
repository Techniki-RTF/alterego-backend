using AlterEgo.Core.Entities;

namespace AlterEgo.Core.Repositories;

public interface IDialogContextsRepository
{
    Task<DialogContextEntity?> GetByDialogIdAsync(long dialogId, CancellationToken cancellationToken = default);
    Task AddAsync(DialogContextEntity context, CancellationToken cancellationToken = default);
    void Remove(DialogContextEntity context);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
