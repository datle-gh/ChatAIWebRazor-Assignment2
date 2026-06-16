using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

[Authorize(Roles = "Admin,Teacher")]
public sealed class SubjectManagementHub : Hub
{
    public Task JoinSubjectIndex()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, SubjectManagementGroups.Index);
    }

    [Authorize(Roles = "Admin")]
    public Task JoinSubjectMembers(int subjectId)
    {
        if (subjectId <= 0)
        {
            throw new HubException("Môn học không hợp lệ.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, SubjectManagementGroups.ForMembers(subjectId));
    }
}
