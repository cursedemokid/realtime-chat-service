using ChatService.Api.Authentication;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Api.Services.Clients;

public sealed class LocalFileStorageService(ConversationDbContext db, IHttpContextAccessor http,
    IWebHostEnvironment environment) : IFileStorageService, IAttachmentAccess
{
    private string DirectoryPath => Path.Combine(environment.ContentRootPath, "uploads");

    private (ChatUserId Id, ParticipantType Type) Current()
    {
        if (!ChatPrincipal.TryResolve(http.HttpContext?.User, out var id, out var type))
            throw new ForbiddenException("An authenticated participant is required.");
        return (id, type);
    }

    public async Task<UploadedFileDto> UploadAsync(Stream content, string fileName, string contentType,
        string uploadedBy, CancellationToken cancellationToken = default)
    {
        var user = Current();
        var file = new StoredFile { Id = Guid.NewGuid(), Owner = $"{user.Type}:{user.Id}",
            FileName = Path.GetFileName(fileName), ContentType = "application/octet-stream" };
        Directory.CreateDirectory(DirectoryPath);
        var path = Path.Combine(DirectoryPath, file.Id.ToString("N"));
        try
        {
            await using (var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(target, cancellationToken);
                file.Size = target.Length;
            }
            db.Set<StoredFile>().Add(file);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        return new UploadedFileDto { Id = file.Id, FileName = file.FileName, ContentType = file.ContentType,
            Size = file.Size, DownloadUrl = $"/api/files/{file.Id}/download" };
    }

    public async Task<StoredFile> RequireAccessAsync(Guid id, ChatUserId userId, ParticipantType type, CancellationToken ct)
    {
        var file = await db.Set<StoredFile>().FindAsync([id], ct)
            ?? throw new ForbiddenException("File is unavailable.");
        if (file.Owner == $"{type}:{userId}") return file;
        var conversationIds = db.Messages.Where(m => m.Content.Attachments.Any(a => a.Id == id) && m.DeletedAt == null)
            .Select(m => m.ConversationId);
        var permitted = await db.Conversations.AnyAsync(c => conversationIds.Contains(c.Id) &&
            c.Participants.Any(p => p.UserId == userId && p.Type == type && p.LeftAt == null), ct);
        if (!permitted) throw new ForbiddenException("File is unavailable.");
        return file;
    }

    public async Task<string> GetDownloadUrlAsync(Guid fileId, int ttlSeconds = 3600, CancellationToken cancellationToken = default)
    {
        var user = Current();
        await RequireAccessAsync(fileId, user.Id, user.Type, cancellationToken);
        return $"/api/files/{fileId}/download";
    }

    public async Task ValidateAsync(IEnumerable<MessageAttachmentDto>? attachments, ChatUserId userId,
        ParticipantType type, CancellationToken cancellationToken)
    {
        if (attachments is null) return;
        foreach (var attachment in attachments)
        {
            if (!Guid.TryParse(attachment.Id, out var id)) throw new ForbiddenException("Invalid attachment.");
            var file = await RequireAccessAsync(id, userId, type, cancellationToken);
            // Persist trusted metadata; clients cannot attach arbitrary external URLs.
            attachment.AttachmentType = ChatService.Domain.Enums.AttachmentType.Document;
            attachment.Name = file.FileName;
            attachment.Size = file.Size;
            attachment.MimeType = file.ContentType;
            attachment.Url = $"/api/files/{id}/download";
        }
    }

    public string GetPath(Guid id) => Path.Combine(DirectoryPath, id.ToString("N"));
}
