using BusinessLogic.DTOs.Requests;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Pages.SystemSettings;

[Authorize(Roles = "Admin")]
public sealed class IndexModel : AppPageModel
{
    private readonly ISystemSettingsService _settingsService;

    public IndexModel(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public SystemSettingsViewModel ViewModel { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSettingsAsync(cancellationToken);

        if (TempData["SuccessMessage"] != null)
        {
            ViewModel.SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var settings = new SystemSettingsDto
        {
            LlmProvider = ViewModel.LlmProvider,
            GeminiApiKey = ViewModel.GeminiApiKey,
            GeminiModel = ViewModel.GeminiModel,
            OpenAiApiKey = ViewModel.OpenAiApiKey,
            OpenAiModel = ViewModel.OpenAiModel,
            EmbeddingProvider = ViewModel.EmbeddingProvider,
            EmbeddingModel = ViewModel.EmbeddingModel,
            TopK = ViewModel.TopK,
            SimilarityThreshold = ViewModel.SimilarityThreshold,
            MaxCitationSnippetLength = ViewModel.MaxCitationSnippetLength,
            ChatSystemPrompt = ViewModel.ChatSystemPrompt,
            EvaluationSystemPrompt = ViewModel.EvaluationSystemPrompt
        };

        await _settingsService.SaveSettingsAsync(settings, cancellationToken);

        TempData["SuccessMessage"] = "Lưu cấu hình hệ thống thành công!";
        return RedirectToPage("/SystemSettings/Index");
    }

    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        ViewModel = new SystemSettingsViewModel
        {
            LlmProvider = settings.LlmProvider,
            GeminiApiKey = settings.GeminiApiKey,
            GeminiModel = settings.GeminiModel,
            OpenAiApiKey = settings.OpenAiApiKey,
            OpenAiModel = settings.OpenAiModel,
            EmbeddingProvider = settings.EmbeddingProvider,
            EmbeddingModel = settings.EmbeddingModel,
            TopK = settings.TopK,
            SimilarityThreshold = settings.SimilarityThreshold,
            MaxCitationSnippetLength = settings.MaxCitationSnippetLength,
            ChatSystemPrompt = settings.ChatSystemPrompt,
            EvaluationSystemPrompt = settings.EvaluationSystemPrompt
        };
    }
}
