namespace AlterEgo.Core.Services;

public interface ILlmService
{
    Task<string> GenerateTextAsync(string message, CancellationToken cancellationToken = default);
}
