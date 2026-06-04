using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Pages.SystemSettings;

[Authorize(Roles = "Admin")]
public sealed class TestConnectionModel : AppPageModel
{
    private readonly ISystemSettingsService _settingsService;

    public TestConnectionModel(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<IActionResult> OnPostAsync(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _settingsService.TestConnectionAsync(
            request.Provider,
            request.ApiKey,
            request.Model,
            cancellationToken);
        return new JsonResult(new { success = result.Success, message = result.Message });
    }
}

public sealed class TestConnectionRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
