using System.ComponentModel.DataAnnotations;

namespace AlterEgo.Api.Dtos;

public record RefreshRequest(
    [Required]
    string RefreshToken
);
