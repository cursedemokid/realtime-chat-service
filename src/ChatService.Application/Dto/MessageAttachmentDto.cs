using ChatService.Domain.Enums;

namespace ChatService.Application.Dto;

public class MessageAttachmentDto
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public long? Size { get; set; }
    public string? MimeType { get; set; }
    public AttachmentType? AttachmentType { get; set; }
    public string Url { get; set; } = null!;
}