namespace BusinessLogic.Services.Interfaces;

public interface ILlmService
{
    string ModelName { get; }

    Task<string> GenerateAnswerAsync(string prompt, CancellationToken cancellationToken = default);

    Task<LlmResponse> GenerateAnswerWithUsageAsync(string prompt, CancellationToken cancellationToken = default);
}

public sealed record LlmResponse(
    string Answer,
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens);
