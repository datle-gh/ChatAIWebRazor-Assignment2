using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Pages.Subject;

[Authorize(Roles = "Admin")]
public sealed class RemoveMemberModel : AppPageModel
{
    private readonly ISubjectService _subjectService;

    public RemoveMemberModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public async Task<IActionResult> OnPostAsync(
        int subjectId,
        int enrollmentId,
        CancellationToken cancellationToken)
    {
        var result = await _subjectService.RemoveSubjectMemberAsync(
            subjectId,
            enrollmentId,
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToPage("/Subject/Members", new { id = subjectId });
    }
}
