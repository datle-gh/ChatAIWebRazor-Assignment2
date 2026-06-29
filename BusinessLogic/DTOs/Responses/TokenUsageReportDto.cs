namespace BusinessLogic.DTOs.Responses;

public sealed record TokenUsageReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int RequestCount,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal AverageTokensPerRequest,
    IReadOnlyList<TokenUsageByModelDto> Models,
    IReadOnlyList<DailyTokenUsageDto> DailyUsage);

public sealed record TokenUsageByModelDto(
    string ModelName,
    int RequestCount,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal AverageTokensPerRequest);

public sealed record DailyTokenUsageDto(
    DateOnly Date,
    string ModelName,
    int RequestCount,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens);
