using BusinessLogic.DTOs.Responses;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Presentation.Models;

namespace Presentation.Pages.Subject;

[Authorize(Roles = "Admin,Teacher")]
public sealed class IndexModel : AppPageModel
{
    private readonly ISubjectService _subjectService;

    public IndexModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public SubjectPageViewModel ViewModel { get; set; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetManagementSubjectsAsync(
            GetCurrentUserId(),
            GetCurrentUserRole(),
            null,
            cancellationToken);

        ViewModel = new SubjectPageViewModel
        {
            IsAdmin = User.IsInRole("Admin"),
            Subjects = subjects.Select(MapSubject).ToList()
        };
    }

    private static SubjectViewModel MapSubject(SubjectDto s)
    {
        return new SubjectViewModel
        {
            Id = s.Id,
            SubjectCode = s.SubjectCode,
            SubjectName = s.SubjectName,
            Description = s.Description,
            DocumentCount = s.DocumentCount,
            IndexedDocumentCount = s.IndexedDocumentCount,
            StudentCount = s.StudentCount,
            TeacherCount = s.TeacherCount,
            CreatedAt = s.CreatedAt,
            CreatedById = s.CreatedById,
            CreatedByName = s.CreatedByName,
            IsTeacherEnrolled = s.IsTeacherEnrolled,
            CanManage = s.CanManage,
            TeacherNames = s.TeacherNames,
            MemberNames = s.MemberNames
        };
    }
}
