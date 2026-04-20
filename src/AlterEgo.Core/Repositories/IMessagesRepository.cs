using AlterEgo.Core.Entities;

namespace AlterEgo.Core.Repositories;

public interface IMessagesRepository
{
    Task<MessageEntity?> GetByTelegramMessageIdAsync(long dialogId, long telegramMessageId, CancellationToken cancellationToken = default);
    Task<MessageEntity?> GetByCoverTextHashAsync(long dialogId, long senderTelegramId, string coverTextHash, DateTimeOffset createdAt, CancellationToken cancellationToken = default);
    Task<List<MessageEntity>> GetByDialogIdAsync(long dialogId, DateTimeOffset? beforeCreatedAt, int limit, CancellationToken cancellationToken = default);
    Task<List<MessageEntity>> GetAfterMessageIdAsync(long dialogId, long? afterMessageId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long dialogId, long telegramMessageId, CancellationToken cancellationToken = default);
    Task AddAsync(MessageEntity message, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
