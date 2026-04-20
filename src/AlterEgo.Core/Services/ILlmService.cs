namespace AlterEgo.Core.Services;

public interface ILlmService
{
    Task<string> GenerateTextAsync(string message, CancellationToken cancellationToken = default);
    Task<string> GenerateTextAsync(
        string message,
        string? dialogContext,
        IReadOnlyList<string> recentMessages,
        CancellationToken cancellationToken = default);
}
