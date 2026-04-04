using System.ComponentModel.DataAnnotations;

namespace AlterEgo.Api.Dtos;

public record SetPasswordRequest(
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string NewPassword
);
