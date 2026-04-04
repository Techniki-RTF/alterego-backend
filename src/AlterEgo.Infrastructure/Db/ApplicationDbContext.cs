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
    }
}