using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlterEgo.Core.Entities;

public class DialogContextEntity
{
    [Key]
    [Column("dialog_id")]
    [Required]
    public required long DialogId { get; set; }

    [Column("context_notes")]
    [Required]
    public required string ContextNotes { get; set; }

    [Column("recent_cover_messages")]
    [Required]
    public required string RecentCoverMessages { get; set; }

    [Column("created_at")]
    [Required]
    public required DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    [Required]
    public required DateTimeOffset UpdatedAt { get; set; }
}
