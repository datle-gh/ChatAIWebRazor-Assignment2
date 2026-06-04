using BusinessLogic.DTOs.Requests;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Pages.Subject;

[Authorize(Roles = "Admin")]
public sealed class AddMemberModel : AppPageModel
{
    private readonly ISubjectService _subjectService;

    public AddMemberModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public async Task<IActionResult> OnPostAsync(
        int subjectId,
        AddSubjectMemberViewModel addMember,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn người dùng và role hợp lệ.";
            return RedirectToPage("/Subject/Members", new { id = subjectId });
        }

        var result = await _subjectService.AddSubjectMemberAsync(
            new AddSubjectMemberRequestDto(subjectId, addMember.UserId, addMember.RoleInClass),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToPage("/Subject/Members", new { id = subjectId });
    }
}
