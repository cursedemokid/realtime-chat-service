using ChatService.Api.Authentication;
using ChatService.Application.Services;
using ChatService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Route("api/Chat")]
public class AttachmentsController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public AttachmentsController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File is required." });

        var current = GetCurrentParticipant();
        await using var stream = file.OpenReadStream();
        var result = await _fileStorageService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            $"{current.Type}:{current.Id}",
            cancellationToken);

        return Created($"/api/Chat/{result.Id}/url", new
        {
            fileId = result.Id,
            result.FileName,
            result.ContentType,
            result.Size,
            url = result.DownloadUrl
        });
    }

    [HttpGet("{fileId:guid}/url")]
    public async Task<ActionResult> GetUrl(Guid fileId, [FromQuery] int ttlSeconds = 3600, CancellationToken cancellationToken = default)
    {
        var url = await _fileStorageService.GetDownloadUrlAsync(fileId, ttlSeconds, cancellationToken);
        return Ok(new { fileId, url });
    }

    private (long Id, ParticipantType Type) GetCurrentParticipant()
    {
        if (!ChatPrincipal.TryResolve(User, out var userId, out var participantType))
            throw new UnauthorizedAccessException("Unauthorized");
        return (userId.UserId, participantType);
    }
}
