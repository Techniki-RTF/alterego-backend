namespace AlterEgo.Api.Dtos;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
