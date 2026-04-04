using AlterEgo.Core.Enums;

namespace AlterEgo.Api.Dtos;

public record UserResponse(
    Guid Id,
    string Username,
    long TelegramId,
    Role Role,
    DateTimeOffset CreatedAt
);
