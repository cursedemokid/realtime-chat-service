using ChatService.Domain.Enums;

namespace ChatService.Application.Dto;

public class ConversationSummaryDto
{
    public Guid Id { get; set; }
    public ConversationKind Kind { get; set; }
    public ConversationStatus Status { get; set; }
    public string? ContextReference { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AvatarUrl { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsMuted { get; set; }
    public bool IsWriteBlocked { get; set; }
    public bool IsPinned { get; set; }
    public MessageDto? LastMessage { get; set; }
}
