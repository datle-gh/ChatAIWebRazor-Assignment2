using System.Security.Claims;
using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize(Roles = "Admin,Teacher")]
public sealed class SubjectController : Controller
{
    private readonly ISubjectService _subjectService;

    public SubjectController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var currentUserRole = GetCurrentUserRole();
        var subjects = await _subjectService.GetManagementSubjectsAsync(
            GetCurrentUserId(),
            currentUserRole,
            null,
            cancellationToken);

        var model = new SubjectPageViewModel
        {
            IsAdmin = User.IsInRole("Admin"),
            Subjects = subjects.Select(MapSubject).ToList()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new CreateSubjectViewModel
        {
            TeacherOptions = await GetTeacherOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateSubjectViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.TeacherOptions = await GetTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        var result = await _subjectService.CreateSubjectAsync(
            new CreateSubjectRequestDto(
                model.SubjectCode,
                model.SubjectName,
                model.Description,
                GetCurrentUserId(),
                GetCurrentUserRole(),
                model.HeadTeacherId),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.TeacherOptions = await GetTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id, cancellationToken);
        if (subject is null)
        {
            return NotFound("Không tìm thấy môn học.");
        }

        return View(new EditSubjectViewModel
        {
            Id = subject.Id,
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            Description = subject.Description,
            HeadTeacherId = subject.CreatedById,
            TeacherOptions = await GetTeacherOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditSubjectViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.TeacherOptions = await GetTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        var result = await _subjectService.UpdateSubjectAsync(
            new UpdateSubjectRequestDto(
                model.Id,
                model.SubjectCode,
                model.SubjectName,
                model.Description,
                model.HeadTeacherId),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            model.TeacherOptions = await GetTeacherOptionsAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Members(int id, CancellationToken cancellationToken)
    {
        var members = await _subjectService.GetSubjectMembersAsync(id, cancellationToken);
        if (members is null)
        {
            return NotFound("Không tìm thấy môn học.");
        }

        return View(MapMembers(members));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(
        int subjectId,
        AddSubjectMemberViewModel addMember,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn người dùng và role hợp lệ.";
            return RedirectToAction(nameof(Members), new { id = subjectId });
        }

        var result = await _subjectService.AddSubjectMemberAsync(
            new AddSubjectMemberRequestDto(subjectId, addMember.UserId, addMember.RoleInClass),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Members), new { id = subjectId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(
        int subjectId,
        int enrollmentId,
        CancellationToken cancellationToken)
    {
        var result = await _subjectService.RemoveSubjectMemberAsync(
            subjectId,
            enrollmentId,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Members), new { id = subjectId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportMembers(
        int subjectId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file CSV/XLS/XLSX.";
            return RedirectToAction(nameof(Members), new { id = subjectId });
        }

        await using var stream = file.OpenReadStream();
        var result = await _subjectService.ImportSubjectMembersAsync(
            new ImportSubjectMembersRequestDto(subjectId, stream, file.FileName),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Members), new { id = subjectId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _subjectService.DeleteSubjectAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<SubjectMemberCandidateViewModel>> GetTeacherOptionsAsync(
        CancellationToken cancellationToken)
    {
        var candidates = await _subjectService.GetMemberCandidatesAsync(cancellationToken);
        return candidates
            .Where(candidate => string.Equals(candidate.Role, "Teacher", StringComparison.OrdinalIgnoreCase))
            .Select(MapCandidate)
            .ToList();
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

    private static SubjectMembersViewModel MapMembers(SubjectMembersDto dto)
    {
        return new SubjectMembersViewModel
        {
            SubjectId = dto.SubjectId,
            SubjectCode = dto.SubjectCode,
            SubjectName = dto.SubjectName,
            HeadTeacherId = dto.HeadTeacherId,
            HeadTeacherName = dto.HeadTeacherName,
            Members = dto.Members.Select(member => new SubjectMemberViewModel
            {
                EnrollmentId = member.EnrollmentId,
                UserId = member.UserId,
                FullName = member.FullName,
                Email = member.Email,
                RoleInClass = member.RoleInClass,
                EnrolledAt = member.EnrolledAt,
                IsHeadTeacher = member.IsHeadTeacher
            }).ToList(),
            Candidates = dto.Candidates.Select(MapCandidate).ToList()
        };
    }

    private static SubjectMemberCandidateViewModel MapCandidate(SubjectMemberCandidateDto candidate)
    {
        return new SubjectMemberCandidateViewModel
        {
            UserId = candidate.UserId,
            FullName = candidate.FullName,
            Email = candidate.Email,
            Role = candidate.Role
        };
    }
}
