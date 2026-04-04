using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AlterEgo.Core.Enums;

namespace AlterEgo.Core.Entities;

public class UserEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    
    [Column("username")]
    [MaxLength(50)]
    [Required]
    public required string Username { get; set; }
    
    [Column("telegram_id")]
    [Required]
    public required long TelegramId { get; set; }
    
    [Column("role")]
    [Required]
    public Role Role { get; set; } = Role.Default;
    
    [Column("password_hash")]
    [Required]
    public required string PasswordHash { get; set; }
    
    [Column("created_at")]
    [Required]
    public required DateTimeOffset CreatedAt { get; set; }
}