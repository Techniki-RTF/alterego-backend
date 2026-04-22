using AlterEgo.Core.Entities;
using AlterEgo.Core.Repositories;
using AlterEgo.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace AlterEgo.Infrastructure.Repositories;

public class MessagesRepository : IMessagesRepository
{
    private readonly ApplicationDbContext _context;

    public MessagesRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MessageEntity?> GetByTelegramMessageIdAsync(long dialogId, long telegramMessageId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .FirstOrDefaultAsync(x => x.DialogId == dialogId && x.TelegramMessageId == telegramMessageId, cancellationToken);
    }

    public async Task<MessageEntity?> GetByCoverTextHashAsync(long dialogId, long senderTelegramId, string coverTextHash, DateTimeOffset createdAt, CancellationToken cancellationToken = default)
    {
        var tolerance = TimeSpan.FromMilliseconds(500);
        var minTime = createdAt - tolerance;
        var maxTime = createdAt + tolerance;

        return await _context.Messages
            .Where(x => x.DialogId == dialogId
                        && x.SenderTelegramId == senderTelegramId
                        && x.CoverTextHash == coverTextHash
                        && x.CreatedAt >= minTime
                        && x.CreatedAt <= maxTime)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MessageEntity>> GetByDialogIdAsync(long dialogId, DateTimeOffset? beforeCreatedAt, int limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Where(x => x.DialogId == dialogId);

        if (beforeCreatedAt.HasValue)
        {
            query = query.Where(x => x.CreatedAt < beforeCreatedAt.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MessageEntity>> GetAfterCreatedAtAsync(long dialogId, DateTimeOffset? afterCreatedAt, CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Where(x => x.DialogId == dialogId);

        if (afterCreatedAt.HasValue)
        {
            query = query.Where(x => x.CreatedAt > afterCreatedAt.Value);
        }

        return await query
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(long dialogId, long telegramMessageId, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .AnyAsync(x => x.DialogId == dialogId && x.TelegramMessageId == telegramMessageId, cancellationToken);
    }

    public async Task AddAsync(MessageEntity message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
