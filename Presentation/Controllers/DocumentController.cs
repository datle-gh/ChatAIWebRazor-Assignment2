using System.Security.Claims;
using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessLogic.Infrastructure.Settings;
using BusinessLogic.Infrastructure.Interfaces;
using BusinessLogic.Services.Interfaces;
using BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
public sealed class DocumentController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly UploadSettings _uploadSettings;

    public DocumentController(
        IDocumentService documentService,
        UploadSettings uploadSettings)
    {
        _documentService = documentService;
        _uploadSettings = uploadSettings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        int? subjectId,
        DocumentStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentsAsync(
            new DocumentListRequestDto(
                searchTerm,
                subjectId,
                status,
                GetCurrentUserId(),
                User.FindFirstValue(ClaimTypes.Role)),
            cancellationToken);
        var uploadSubjects = await _documentService.GetUploadSubjectOptionsAsync(
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            cancellationToken);

        return View(new DocumentIndexViewModel
        {
            SearchTerm = searchTerm,
            SubjectId = subjectId,
            Status = status,
            CanUploadCurrentSubject = subjectId.HasValue
                && uploadSubjects.Any(subject => subject.Id == subjectId.Value),
            Subjects = MapSubjects(result.Subjects),
            Documents = result.Documents.Select(MapDocumentListItem).ToList()
        });
    }

    [HttpGet]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Upload(int? subjectId, CancellationToken cancellationToken)
    {
        var subjects = await GetSubjectOptionsAsync(cancellationToken);
        if (subjects.Count == 0)
        {
            TempData["ErrorMessage"] = "Chỉ trưởng bộ môn mới có quyền tải tài liệu lên.";
            return RedirectToAction(nameof(Index));
        }

        var selectedSubjectId = subjectId.HasValue && subjects.Any(subject => subject.Id == subjectId.Value)
            ? subjectId
            : null;

        return View(new DocumentUploadViewModel
        {
            SubjectId = selectedSubjectId,
            UploadId = Guid.NewGuid().ToString("N"),
            MaxFileSizeMb = _uploadSettings.MaxFileSizeMb,
            MaxFilesPerBatch = _uploadSettings.MaxFilesPerBatch,
            MaxBatchSizeMb = _uploadSettings.MaxBatchSizeMb,
            Subjects = subjects
        });
    }

    [HttpPost]
    [Authorize(Roles = "Teacher")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        DocumentUploadViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Files.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Files), "Vui lòng chọn tài liệu để tải lên.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                succeeded = false,
                message = GetFirstModelError()
            });
        }

        var fileRequests = new List<DocumentBatchUploadFileRequest>();
        var streams = new List<Stream>();

        try
        {
            foreach (var file in model.Files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                fileRequests.Add(new DocumentBatchUploadFileRequest(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length));
            }

            var result = await _documentService.UploadBatchAndIndexAsync(
                new DocumentBatchUploadRequest(
                    string.IsNullOrWhiteSpace(model.UploadId) ? Guid.NewGuid().ToString("N") : model.UploadId,
                    fileRequests,
                    model.SubjectId.GetValueOrDefault(),
                    GetCurrentUserId(),
                    User.FindFirstValue(ClaimTypes.Role),
                    model.Title),
                cancellationToken);

            return Json(new
            {
                succeeded = result.Succeeded,
                message = result.Message,
                items = result.Items.Select(item => new
                {
                    succeeded = item.Succeeded,
                    documentId = item.DocumentId,
                    fileName = item.FileName,
                    message = item.Message
                })
            });
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(int id, int? subjectId, CancellationToken cancellationToken)
    {
        var result = await _documentService.VerifyAndIndexAsync(
            id,
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            CancellationToken.None);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return Json(new
            {
                succeeded = result.Succeeded,
                message = result.Message,
                documentId = result.DocumentId
            });
        }

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { subjectId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        int? subjectId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.RejectAsync(
            id,
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            reason,
            cancellationToken);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return Json(new
            {
                succeeded = result.Succeeded,
                message = result.Message,
                documentId = result.DocumentId
            });
        }

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index), new { subjectId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await _documentService.GetDocumentFileAsync(
            id,
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            cancellationToken);

        if (file is null)
        {
            TempData["ErrorMessage"] = "Kh\u00f4ng t\u00ecm th\u1ea5y t\u00e0i li\u1ec7u ho\u1eb7c b\u1ea1n kh\u00f4ng c\u00f3 quy\u1ec1n t\u1ea3i xu\u1ed1ng.";
            return RedirectToAction(nameof(Index));
        }

        var (filePath, originalFileName) = file.Value;
        var mimeType = GetMimeType(originalFileName);
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fileStream, mimeType, originalFileName);
    }

    [HttpGet]
    public async Task<IActionResult> Chunks(
        int id,
        int page = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _documentService.GetDocumentChunksAsync(
            id,
            page,
            pageSize,
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            cancellationToken);

        if (result is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài liệu hoặc bạn không có quyền xem phân đoạn.";
            return RedirectToAction(nameof(Index));
        }

        return View(MapDocumentChunks(result));
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf"  => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _       => "application/octet-stream"
        };
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private string GetFirstModelError()
    {
        return ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? "Dữ liệu tải lên không hợp lệ.";
    }

    private async Task<IReadOnlyList<SubjectOptionViewModel>> GetSubjectOptionsAsync(
        CancellationToken cancellationToken)
    {
        var subjects = await _documentService.GetUploadSubjectOptionsAsync(
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            cancellationToken);
        return MapSubjects(subjects);
    }

    private static IReadOnlyList<SubjectOptionViewModel> MapSubjects(
        IReadOnlyList<SubjectOptionDto> subjects)
    {
        return subjects
            .Select(subject => new SubjectOptionViewModel
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName
            })
            .ToList();
    }

    private static DocumentListItemViewModel MapDocumentListItem(DocumentListItemDto document)
    {
        return new DocumentListItemViewModel
        {
            Id = document.Id,
            SubjectId = document.SubjectId,
            SubjectName = document.SubjectName,
            Title = document.Title,
            OriginalFileName = document.OriginalFileName,
            FileType = document.FileType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedByName = document.UploadedByName,
            Status = document.Status,
            ErrorMessage = document.ErrorMessage,
            UploadedAt = document.UploadedAt,
            IndexedAt = document.IndexedAt,
            ChunkCount = document.ChunkCount,
            TotalTokenCount = document.TotalTokenCount,
            EmbeddingModel = document.EmbeddingModel,
            PreviewChunkIndex = document.PreviewChunkIndex,
            PreviewContent = document.PreviewContent
        };
    }

    private static DocumentChunksViewModel MapDocumentChunks(DocumentChunksDto document)
    {
        return new DocumentChunksViewModel
        {
            DocumentId = document.DocumentId,
            SubjectId = document.SubjectId,
            SubjectName = document.SubjectName,
            Title = document.Title,
            OriginalFileName = document.OriginalFileName,
            FileType = document.FileType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedByName = document.UploadedByName,
            Status = document.Status,
            UploadedAt = document.UploadedAt,
            IndexedAt = document.IndexedAt,
            TotalChunks = document.TotalChunks,
            TotalTokens = document.TotalTokens,
            CurrentPage = document.CurrentPage,
            PageSize = document.PageSize,
            TotalPages = document.TotalPages,
            Chunks = document.Chunks.Select(chunk => new DocumentChunkItemViewModel
            {
                Id = chunk.Id,
                ChunkIndex = chunk.ChunkIndex,
                PageNumber = chunk.PageNumber,
                SlideNumber = chunk.SlideNumber,
                TokenCount = chunk.TokenCount,
                Content = chunk.Content,
                CreatedAt = chunk.CreatedAt
            }).ToList()
        };
    }
}
