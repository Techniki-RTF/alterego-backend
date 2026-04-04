using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlterEgo.Core.Entities;

public class RefreshTokenEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("token")]
    [Required]
    public required string Token { get; set; }
    
    [Column("user_id")]
    [Required]
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }
    
    [Column("expires_at")]
    [Required]
    public required DateTimeOffset ExpiresAt { get; set; }
    
    [Column("created_at")]
    [Required]
    public required DateTimeOffset CreatedAt { get; set; }
    
    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }
    
    [NotMapped]
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    
    [NotMapped]
    public bool IsRevoked => RevokedAt.HasValue;
    
    [NotMapped]
    public bool IsActive => !IsRevoked && !IsExpired;
}
