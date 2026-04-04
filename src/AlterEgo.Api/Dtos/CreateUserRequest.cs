using System.ComponentModel.DataAnnotations;

namespace AlterEgo.Api.Dtos;

public record CreateUserRequest(
    [Required]
    [StringLength(50, MinimumLength = 3)]
    string Username,
    
    [Required]
    long TelegramId,
    
    [Required]
    [StringLength(100, MinimumLength = 6)]
    string Password
);
