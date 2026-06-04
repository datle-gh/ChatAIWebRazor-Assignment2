using BusinessObject.Entities;

namespace DataAccess.Repositories.Interfaces;

public interface IRagasBenchmarkResultRepository
{
    Task<IReadOnlyList<RagasBenchmarkResult>> GetByEvaluationQuestionIdsAsync(
        IEnumerable<int> evaluationQuestionIds,
        CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<RagasBenchmarkResult> results, CancellationToken cancellationToken = default);
    Task<int> CountBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<RagasBenchmarkResult?> GetLatestBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RagasBenchmarkResult>> GetLatestRunBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);
}
