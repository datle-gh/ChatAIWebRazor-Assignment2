using BusinessLogic.DTOs.Responses;
using BusinessLogic.Services.Interfaces;
using DataAccess.Repositories.Interfaces;

namespace BusinessLogic.Services.Implementations;

public sealed class TokenUsageReportService : ITokenUsageReportService
{
    private readonly IChatRepository _chatRepository;

    public TokenUsageReportService(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<TokenUsageReportDto> GetLastSevenDaysAsync(
        CancellationToken cancellationToken = default)
    {
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.Date.AddDays(-6);
        var messages = await _chatRepository.GetAssistantMessagesByDateRangeAsync(
            fromUtc,
            toUtc.AddDays(1),
            cancellationToken);

        var modelSummaries = messages
            .GroupBy(message => message.ModelName ?? "Unknown")
            .Select(group =>
            {
                var requestCount = group.Count();
                var promptTokens = group.Sum(message => message.PromptTokens ?? 0);
                var completionTokens = group.Sum(message => message.CompletionTokens ?? 0);
                var totalTokens = promptTokens + completionTokens;

                return new TokenUsageByModelDto(
                    group.Key,
                    requestCount,
                    promptTokens,
                    completionTokens,
                    totalTokens,
                    CalculateAverage(totalTokens, requestCount));
            })
            .OrderByDescending(summary => summary.TotalTokens)
            .ThenBy(summary => summary.ModelName)
            .ToList();

        var dailyUsage = messages
            .GroupBy(message => new
            {
                Date = DateOnly.FromDateTime(message.CreatedAt.Date),
                ModelName = message.ModelName ?? "Unknown"
            })
            .Select(group =>
            {
                var promptTokens = group.Sum(message => message.PromptTokens ?? 0);
                var completionTokens = group.Sum(message => message.CompletionTokens ?? 0);

                return new DailyTokenUsageDto(
                    group.Key.Date,
                    group.Key.ModelName,
                    group.Count(),
                    promptTokens,
                    completionTokens,
                    promptTokens + completionTokens);
            })
            .OrderBy(day => day.Date)
            .ThenBy(day => day.ModelName)
            .ToList();

        var totalPromptTokens = modelSummaries.Sum(summary => summary.PromptTokens);
        var totalCompletionTokens = modelSummaries.Sum(summary => summary.CompletionTokens);
        var totalTokens = totalPromptTokens + totalCompletionTokens;
        var requestCount = modelSummaries.Sum(summary => summary.RequestCount);

        return new TokenUsageReportDto(
            fromUtc,
            toUtc,
            requestCount,
            totalPromptTokens,
            totalCompletionTokens,
            totalTokens,
            CalculateAverage(totalTokens, requestCount),
            modelSummaries,
            dailyUsage);
    }

    private static decimal CalculateAverage(int totalTokens, int requestCount)
    {
        return requestCount == 0
            ? 0
            : Math.Round(totalTokens / (decimal)requestCount, 2);
    }
}
