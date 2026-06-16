using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;

namespace Presentation.Services;

public sealed class SignalRSubjectRealtimeNotifier : ISubjectRealtimeNotifier
{
    private readonly IHubContext<SubjectManagementHub> _hubContext;

    public SignalRSubjectRealtimeNotifier(IHubContext<SubjectManagementHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySubjectChangedAsync(
        int subjectId,
        string action,
        string message,
        CancellationToken cancellationToken = default)
    {
        var payload = CreatePayload(subjectId, action, message);
        await _hubContext.Clients
            .Group(SubjectManagementGroups.Index)
            .SendAsync(action, payload, cancellationToken);
        await _hubContext.Clients
            .Group(SubjectManagementGroups.ForMembers(subjectId))
            .SendAsync(action, payload, cancellationToken);
    }

    public async Task NotifySubjectMembersChangedAsync(
        int subjectId,
        string action,
        string message,
        CancellationToken cancellationToken = default)
    {
        var payload = CreatePayload(subjectId, action, message);
        await _hubContext.Clients
            .Group(SubjectManagementGroups.Index)
            .SendAsync(action, payload, cancellationToken);
        await _hubContext.Clients
            .Group(SubjectManagementGroups.ForMembers(subjectId))
            .SendAsync(action, payload, cancellationToken);
    }

    private static SubjectRealtimeEventDto CreatePayload(
        int subjectId,
        string action,
        string message)
    {
        return new SubjectRealtimeEventDto(
            action,
            subjectId,
            DateTime.UtcNow,
            message);
    }
}
