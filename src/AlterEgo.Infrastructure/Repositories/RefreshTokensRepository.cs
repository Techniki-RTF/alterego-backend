using AlterEgo.Core.Entities;
using AlterEgo.Core.Repositories;
using AlterEgo.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace AlterEgo.Infrastructure.Repositories;

public class RefreshTokensRepository : IRefreshTokensRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokensRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<RefreshTokenEntity>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RefreshTokenEntity refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
