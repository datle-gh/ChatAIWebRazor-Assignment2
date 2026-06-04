using BusinessLogic.Infrastructure;
using BusinessLogic.Infrastructure.Interfaces;
using BusinessLogic.Services.Interfaces;

namespace BusinessLogic.Services.Implementations;

public sealed class ChunkingService : IChunkingService
{
    public IReadOnlyList<DocumentChunkDraft> SplitIntoChunks(IReadOnlyList<ExtractedTextSegment> segments)
    {
        var chunks = new List<DocumentChunkDraft>();

        foreach (var segment in segments.Where(item => !string.IsNullOrWhiteSpace(item.Text)))
        {
            AddChunk(
                chunks,
                segment.Text,
                segment.PageNumber,
                segment.SlideNumber,
                CountApproximateTokens(segment.Text));
        }

        return chunks;
    }

    private void AddChunk(
        ICollection<DocumentChunkDraft> chunks,
        string content,
        int? pageNumber,
        int? slideNumber,
        int tokenCount)
    {
        var cleanedContent = content.Trim();
        if (string.IsNullOrWhiteSpace(cleanedContent))
        {
            return;
        }

        chunks.Add(new DocumentChunkDraft(
            chunks.Count,
            cleanedContent,
            pageNumber,
            slideNumber,
            tokenCount));
    }

    private static int CountApproximateTokens(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }
}
