namespace Presentation.Models;

public sealed class TokenUsageReportViewModel
{
    public DateTime FromUtc { get; set; }

    public DateTime ToUtc { get; set; }

    public int RequestCount { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public decimal AverageTokensPerRequest { get; set; }

    public List<TokenUsageByModelItem> Models { get; set; } = new();

    public List<DailyTokenUsageItem> DailyUsage { get; set; } = new();
}

public sealed class TokenUsageByModelItem
{
    public string ModelName { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public decimal AverageTokensPerRequest { get; set; }
}

public sealed class DailyTokenUsageItem
{
    public DateOnly Date { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}
