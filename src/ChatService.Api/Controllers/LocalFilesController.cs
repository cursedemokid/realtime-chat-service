using ChatService.Api.Authentication;
using ChatService.Api.Services.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatService.Api.Controllers;

[ApiController, Authorize, Route("api/files")]
public sealed class LocalFilesController(LocalFileStorageService storage) : ControllerBase
{
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        if (!ChatPrincipal.TryResolve(User, out var user, out var type)) return Forbid();
        var metadata = await storage.RequireAccessAsync(id, user, type, cancellationToken);
        var path = storage.GetPath(id);
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path, metadata.ContentType, metadata.FileName);
    }
}
