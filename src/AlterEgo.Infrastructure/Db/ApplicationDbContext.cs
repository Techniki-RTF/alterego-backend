using AlterEgo.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlterEgo.Infrastructure.Db;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<UserEntity> Users { get; private set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; private set; }
    public DbSet<MessageEntity> Messages { get; private set; }
    public DbSet<DialogContextEntity> DialogContexts { get; private set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshTokenEntity>()
            .HasIndex(x => x.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshTokenEntity>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MessageEntity>()
            .HasIndex(x => new { x.DialogId, x.TelegramMessageId })
            .IsUnique();

        modelBuilder.Entity<MessageEntity>()
            .HasIndex(x => x.DialogId);

        modelBuilder.Entity<MessageEntity>()
            .HasIndex(x => new { x.DialogId, x.SenderTelegramId, x.CoverTextHash });

        modelBuilder.Entity<DialogContextEntity>()
            .HasKey(x => x.DialogId);

        modelBuilder.Entity<DialogContextEntity>()
            .Property(x => x.DialogId)
            .ValueGeneratedNever();
    }
}
