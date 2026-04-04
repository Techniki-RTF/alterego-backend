using System.ComponentModel.DataAnnotations;

namespace AlterEgo.Api.Dtos;

public record ChangePasswordRequest(
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string CurrentPassword,
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string NewPassword
);
