namespace AlterEgo.Api.Dtos;

public record LlmRequest(string Message);

public record LlmResponse(string Text);
