namespace BusinessLogic.Services.Interfaces;

public interface ILlmService
{
    string ModelName { get; }

    Task<string> GenerateAnswerAsync(string prompt, CancellationToken cancellationToken = default);
}
