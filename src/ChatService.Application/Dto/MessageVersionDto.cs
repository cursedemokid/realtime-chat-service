using ChatService.Domain.Enums;

namespace ChatService.Application.Dto;

public class MessageVersionDto
{
    public int Version { get; set; }
    public string? Text { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
    public long EditorId { get; set; }
    public ParticipantType EditorType { get; set; }
    public DateTime SavedAt { get; set; }
}
