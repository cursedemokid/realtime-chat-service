using ChatService.Domain.Enums;

namespace ChatService.Application.Dto;

public class ConversationAuditEntryDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ConversationAuditAction Action { get; set; }
    public long? ActorId { get; set; }
    public ParticipantType? ActorType { get; set; }
    public long? TargetParticipantId { get; set; }
    public ParticipantType? TargetParticipantType { get; set; }
    public string? Payload { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class ConversationAuditPageDto
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public IReadOnlyList<ConversationAuditEntryDto> Items { get; set; } = Array.Empty<ConversationAuditEntryDto>();
}
