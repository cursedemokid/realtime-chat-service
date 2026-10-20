using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;

namespace ChatService.Domain.ValueObjects;

public sealed class Attachment
{
    public Guid Id { get; }
    public string FileName { get; }
    public long Size { get; }
    public string MimeType { get; }
    public AttachmentType Type { get; }
    public string Url { get; }

    private Attachment()
    {
        Id = Guid.Empty;
        FileName = null!;
        MimeType = null!;
        Url = null!;
    }

    public Attachment(Guid id, string fileName, long size, string mimeType, AttachmentType type, string url)
    {
        if (id == Guid.Empty)
            throw new DomainException("Attachment id is required");
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Attachment url is required");
        Id = id;
        FileName = string.IsNullOrWhiteSpace(fileName) ? id.ToString() : fileName.Trim();
        Size = size;
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim();
        Type = type;
        Url = url.Trim();
    }
}
