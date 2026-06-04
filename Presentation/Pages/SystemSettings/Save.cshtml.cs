using BusinessLogic.DTOs.Requests;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Pages.SystemSettings;

[Authorize(Roles = "Admin")]
public sealed class SaveModel : AppPageModel
{
    private readonly ISystemSettingsService _settingsService;

    public SaveModel(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public SystemSettingsViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/SystemSettings/Index");
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
}
