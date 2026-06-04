using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Pages.RagasEvaluation;

[Authorize(Roles = "Admin")]
public sealed class RunEvaluationModel : AppPageModel
{
    private readonly IRagasEvaluationService _evaluationService;

    public RunEvaluationModel(IRagasEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    public async Task<IActionResult> OnPostAsync(
        [FromBody] RunEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _evaluationService.RunEvaluationAsync(
                request.SubjectId,
                request.EmbeddingModels,
                request.ChunkingStrategies,
                cancellationToken);

            if (result is null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Không thể chạy đánh giá. Vui lòng kiểm tra câu hỏi benchmark."
                });
            }

            return new JsonResult(new { success = true, subjectId = request.SubjectId });
        }
        catch (Exception exception)
        {
            return new JsonResult(new { success = false, message = exception.Message });
        }
    }
}

public sealed class RunEvaluationRequest
{
    public int SubjectId { get; set; }

    public List<string> EmbeddingModels { get; set; } = new();

    public List<string> ChunkingStrategies { get; set; } = new();
}
