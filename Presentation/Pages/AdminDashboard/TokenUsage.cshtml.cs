using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Presentation.Models;

namespace Presentation.Pages.AdminDashboard;

[Authorize(Roles = "Admin")]
public sealed class TokenUsageModel : AppPageModel
{
    private readonly ITokenUsageReportService _tokenUsageReportService;

    public TokenUsageModel(ITokenUsageReportService tokenUsageReportService)
    {
        _tokenUsageReportService = tokenUsageReportService;
    }

    public TokenUsageReportViewModel ViewModel { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var report = await _tokenUsageReportService.GetLastSevenDaysAsync(cancellationToken);
        ViewModel = new TokenUsageReportViewModel
        {
            FromUtc = report.FromUtc,
            ToUtc = report.ToUtc,
            RequestCount = report.RequestCount,
            PromptTokens = report.PromptTokens,
            CompletionTokens = report.CompletionTokens,
            TotalTokens = report.TotalTokens,
            AverageTokensPerRequest = report.AverageTokensPerRequest,
            Models = report.Models.Select(model => new TokenUsageByModelItem
            {
                ModelName = model.ModelName,
                RequestCount = model.RequestCount,
                PromptTokens = model.PromptTokens,
                CompletionTokens = model.CompletionTokens,
                TotalTokens = model.TotalTokens,
                AverageTokensPerRequest = model.AverageTokensPerRequest
            }).ToList(),
            DailyUsage = report.DailyUsage.Select(day => new DailyTokenUsageItem
            {
                Date = day.Date,
                ModelName = day.ModelName,
                RequestCount = day.RequestCount,
                PromptTokens = day.PromptTokens,
                CompletionTokens = day.CompletionTokens,
                TotalTokens = day.TotalTokens
            }).ToList()
        };
    }
}
