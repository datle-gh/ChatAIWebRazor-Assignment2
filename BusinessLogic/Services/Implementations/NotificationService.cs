using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure.Interfaces;
using BusinessLogic.Services.Interfaces;
using BusinessObject.Entities;
using DataAccess.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Implementations;

public sealed class NotificationService : INotificationService
{
    private const string SubjectDeletedType = "SubjectDeleted";

    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationRealtimeNotifier _notificationRealtimeNotifier;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationRealtimeNotifier notificationRealtimeNotifier,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _notificationRealtimeNotifier = notificationRealtimeNotifier;
        _logger = logger;
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(
        int userId,
        int take = 5,
        CancellationToken cancellationToken = default)
    {
        var unreadCount = await _notificationRepository.CountUnreadByUserAsync(userId, cancellationToken);
        var recent = await _notificationRepository.GetRecentByUserAsync(userId, take, cancellationToken);
        return new NotificationSummaryDto(unreadCount, recent.Select(MapNotification).ToList());
    }

    public async Task MarkAsReadAsync(
        int notificationId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAsReadAsync(notificationId, userId, cancellationToken);
        await NotifyUserSafelyAsync(userId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);
        await NotifyUserSafelyAsync(userId, cancellationToken);
    }

    public async Task NotifySubjectDeletedAsync(
        Subject subject,
        CancellationToken cancellationToken = default)
    {
        var recipientIds = subject.SubjectEnrollments
            .Where(enrollment => enrollment.User is { IsActive: true })
            .Select(enrollment => enrollment.UserId)
            .Concat(subject.CreatedBy.HasValue && subject.CreatedByNavigation?.IsActive == true
                ? new[] { subject.CreatedBy.Value }
                : [])
            .Distinct()
            .ToList();
        if (recipientIds.Count == 0)
        {
            return;
        }

        var title = "Môn học đã bị gỡ";
        var message = $"Môn học {subject.SubjectCode} - {subject.SubjectName} đã bị gỡ khỏi hệ thống học tập.";
        var notifications = recipientIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = SubjectDeletedType,
            RelatedSubjectId = subject.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _notificationRepository.AddRangeAsync(notifications, cancellationToken);

        foreach (var userId in recipientIds)
        {
            await NotifyUserSafelyAsync(userId, cancellationToken);
        }
    }

    private async Task NotifyUserSafelyAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await GetSummaryAsync(userId, cancellationToken: cancellationToken);
            await _notificationRealtimeNotifier.NotifyAsync(userId, summary, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not broadcast notifications for user {UserId}", userId);
        }
    }

    private static NotificationDto MapNotification(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.RelatedSubjectId,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt);
    }
}
