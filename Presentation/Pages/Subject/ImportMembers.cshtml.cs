using BusinessLogic.DTOs.Requests;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Pages.Subject;

[Authorize(Roles = "Admin")]
public sealed class ImportMembersModel : AppPageModel
{
    private readonly ISubjectService _subjectService;

    public ImportMembersModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public async Task<IActionResult> OnPostAsync(
        int subjectId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file CSV/XLS/XLSX.";
            return RedirectToPage("/Subject/Members", new { id = subjectId });
        }

        await using var stream = file.OpenReadStream();
        var result = await _subjectService.ImportSubjectMembersAsync(
            new ImportSubjectMembersRequestDto(subjectId, stream, file.FileName),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToPage("/Subject/Members", new { id = subjectId });
    }
}
