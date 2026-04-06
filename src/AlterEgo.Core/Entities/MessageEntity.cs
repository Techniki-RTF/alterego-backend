using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlterEgo.Core.Entities;

public class MessageEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("telegram_message_id")]
    [Required]
    public required long TelegramMessageId { get; set; }

    [Column("sender_telegram_id")]
    [Required]
    public required long SenderTelegramId { get; set; }

    [Column("dialog_id")]
    [Required]
    public required long DialogId { get; set; }

    [Column("original_text")]
    [Required]
    public required string OriginalText { get; set; }

    [Column("cover_text_hash")]
    [MaxLength(64)]
    [Required]
    public required string CoverTextHash { get; set; }

    [Column("created_at")]
    [Required]
    public required DateTimeOffset CreatedAt { get; set; }
}
