using System.Security.Claims;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly ISubjectService _subjectService;

    public DashboardController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "AdminDashboard");
        }

        var userId = GetCurrentUserId();
        var dashboard = await _subjectService.GetStudentDashboardAsync(userId, cancellationToken);

        var model = new DashboardViewModel
        {
            UserName = User.Identity?.Name ?? "Sinh viên",
            SubjectCount = dashboard.SubjectCount,
            ChatSessionCount = dashboard.ChatSessionCount,
            IndexedDocumentCount = dashboard.IndexedDocumentCount,
            RecentCourses = dashboard.RecentCourses
                .Select(c => new RecentCourseViewModel
                {
                    Id = c.Id,
                    SubjectCode = c.SubjectCode,
                    SubjectName = c.SubjectName,
                    Description = c.Description,
                    DocumentCount = c.DocumentCount,
                    IndexedDocumentCount = c.IndexedDocumentCount,
                    ChatSessionCount = c.ChatSessionCount,
                    ProgressPercent = c.ProgressPercent
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SelectSubject(string? filter, CancellationToken cancellationToken)
    {
        var currentUserRole = GetCurrentUserRole();
        var subjects = await _subjectService.GetSelectableSubjectsAsync(
            GetCurrentUserId(),
            currentUserRole,
            null,
            cancellationToken);

        var model = new SelectSubjectViewModel
        {
            SelectedFilter = User.IsInRole("Teacher") ? "enrolled" : "all",
            ShowTeacherFilters = false,
            Subjects = subjects
                .Select(s => new SubjectSelectionItemViewModel
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    Description = s.Description,
                    IndexedDocumentCount = s.IndexedDocumentCount,
                    IsEnrolled = User.IsInRole("Student")
                        ? s.IsStudentEnrolled
                        : s.IsTeacherEnrolled
                })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinSubject(int subjectId, CancellationToken cancellationToken)
    {
        var result = await _subjectService.EnrollStudentAsync(
            subjectId,
            GetCurrentUserId(),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(SelectSubject));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }

}
