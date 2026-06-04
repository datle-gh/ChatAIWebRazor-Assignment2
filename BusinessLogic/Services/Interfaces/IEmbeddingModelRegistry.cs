using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services.Interfaces;

public interface IEmbeddingModelRegistry
{
    IEmbeddingService GetDefault();

    IEmbeddingService GetRequired(string modelKey);

    IReadOnlyList<EmbeddingModelDto> GetAvailableModels(bool benchmarkOnly = false);
}
