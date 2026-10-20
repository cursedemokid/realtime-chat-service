using ChatService.Domain.Enums;

namespace ChatService.Api.Hubs.Dto;

public class ConversationSummaryDto
{
    public string Id { get; set; } = default!;
    public ConversationKind Kind { get; set; }
    public ConversationContextDto? Context { get; set; }
    public List<ConversationParticipantDto> Participants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public LastMessageSummaryDto? LastMessage { get; set; }
    public int? UnreadCountTotal { get; set; }
    public bool? IsMuted { get; set; }
    public bool? IsPinned { get; set; }
    public bool? IsArchived { get; set; }
    public ConversationStatus? Status { get; set; }
}