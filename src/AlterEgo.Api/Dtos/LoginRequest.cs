using System.ComponentModel.DataAnnotations;

namespace AlterEgo.Api.Dtos;

public record LoginRequest(
    [Required]
    string Username,
    
    [Required]
    string Password
);
