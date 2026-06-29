using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services.Interfaces;

public interface ITokenUsageReportService
{
    Task<TokenUsageReportDto> GetLastSevenDaysAsync(CancellationToken cancellationToken = default);
}
