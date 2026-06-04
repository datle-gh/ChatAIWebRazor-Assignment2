using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Pages.Subject;

[Authorize(Roles = "Admin")]
public sealed class DeleteModel : AppPageModel
{
    private readonly ISubjectService _subjectService;

    public DeleteModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        var result = await _subjectService.DeleteSubjectAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToPage("/Subject/Index");
    }
}
