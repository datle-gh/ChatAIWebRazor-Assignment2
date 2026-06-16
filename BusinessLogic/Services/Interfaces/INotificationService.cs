using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;

namespace BusinessLogic.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationSummaryDto> GetSummaryAsync(
        int userId,
        int take = 5,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task NotifySubjectDeletedAsync(
        Subject subject,
        CancellationToken cancellationToken = default);
}
