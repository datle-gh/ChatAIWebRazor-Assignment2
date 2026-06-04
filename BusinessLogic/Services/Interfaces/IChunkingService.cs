using BusinessLogic.Infrastructure;
using BusinessLogic.Infrastructure.Interfaces;

namespace BusinessLogic.Services.Interfaces;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunkDraft> SplitIntoChunks(IReadOnlyList<ExtractedTextSegment> segments);
}

public sealed record DocumentChunkDraft(
    int ChunkIndex,
    string Content,
    int? PageNumber,
    int? SlideNumber,
    int TokenCount);
