using AlterEgo.Core.Entities;
using AlterEgo.Core.Repositories;
using AlterEgo.Infrastructure.Db;
using Microsoft.EntityFrameworkCore;

namespace AlterEgo.Infrastructure.Repositories;

public class DialogContextsRepository : IDialogContextsRepository
{
    private readonly ApplicationDbContext _context;

    public DialogContextsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DialogContextEntity?> GetByDialogIdAsync(long dialogId, CancellationToken cancellationToken = default)
    {
        return await _context.DialogContexts
            .FirstOrDefaultAsync(x => x.DialogId == dialogId, cancellationToken);
    }

    public async Task AddAsync(DialogContextEntity context, CancellationToken cancellationToken = default)
    {
        await _context.DialogContexts.AddAsync(context, cancellationToken);
    }

    public void Remove(DialogContextEntity context)
    {
        _context.DialogContexts.Remove(context);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
