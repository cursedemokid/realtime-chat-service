using ChatService.Domain.Enums;

namespace ChatService.Application.Dto;

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageAuthorDto Author { get; set; } = null!;
    public MessageType Type { get; set; }
    public string? TextMessage { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
    public List<long> ReadBy { get; set; } = new();
    public DateTime CreationDate { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsAuthor { get; set; }
    public MessageReadStatus ReadStatus { get; set; }
    public ReplyToDto? ReplyTo { get; set; }
}
