using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Pages.RagasEvaluation;

[Authorize(Roles = "Admin")]
public sealed class DetailsModel : AppPageModel
{
    private readonly IRagasEvaluationService _evaluationService;

    public DetailsModel(IRagasEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    public RagasRunResultViewModel ViewModel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int subjectId, CancellationToken cancellationToken)
    {
        var result = await _evaluationService.GetLatestRunAsync(subjectId, cancellationToken);
        if (result is null)
        {
            return RedirectToPage("/RagasEvaluation/Index");
        }

        ViewModel = new RagasRunResultViewModel
        {
            SubjectId = result.SubjectId,
            SubjectName = result.SubjectName,
            EmbeddingModel = result.EmbeddingModel,
            LlmModel = result.LlmModel,
            ChunkingStrategy = result.ChunkingStrategy,
            QuestionCount = result.QuestionCount,
            AvgFaithfulness = result.AvgFaithfulness,
            AvgAnswerRelevancy = result.AvgAnswerRelevancy,
            AvgContextPrecision = result.AvgContextPrecision,
            AvgContextRecall = result.AvgContextRecall,
            AvgOverallScore = result.AvgOverallScore,
            RunDate = result.RunDate,
            ModelSummaries = result.ModelSummaries.Select(summary => new RagasModelSummaryItem
            {
                EmbeddingModel = summary.EmbeddingModel,
                LlmModel = summary.LlmModel,
                VectorStore = summary.VectorStore,
                ChunkingStrategy = summary.ChunkingStrategy,
                QuestionCount = summary.QuestionCount,
                AvgFaithfulness = summary.AvgFaithfulness,
                AvgAnswerRelevancy = summary.AvgAnswerRelevancy,
                AvgContextPrecision = summary.AvgContextPrecision,
                AvgContextRecall = summary.AvgContextRecall,
                AvgOverallScore = summary.AvgOverallScore
            }).ToList(),
            Results = result.Results.Select(item => new RagasResultDetailItem
            {
                EmbeddingModel = item.EmbeddingModel,
                ChunkingStrategy = item.ChunkingStrategy,
                Question = item.Question,
                GroundTruthAnswer = item.GroundTruthAnswer,
                GeneratedAnswer = item.GeneratedAnswer,
                Faithfulness = item.Faithfulness,
                AnswerRelevancy = item.AnswerRelevancy,
                ContextPrecision = item.ContextPrecision,
                ContextRecall = item.ContextRecall,
                OverallScore = item.OverallScore
            }).ToList()
        };

        return Page();
    }
}
