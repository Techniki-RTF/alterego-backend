using System.ComponentModel.DataAnnotations;
using AlterEgo.Core.Enums;

namespace AlterEgo.Api.Dtos;

public record ChangeRoleRequest(
    [Required]
    Role Role
);
