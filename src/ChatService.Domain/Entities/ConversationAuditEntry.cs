using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Entities;

public class ConversationAuditEntry
{
    public Guid Id { get; private set; }
    public ConversationId ConversationId { get; private set; } = default!;
    public ConversationAuditAction Action { get; private set; }
    public ChatUserId? ActorId { get; private set; }
    public ParticipantType? ActorType { get; private set; }
    public ChatUserId? TargetParticipantId { get; private set; }
    public ParticipantType? TargetParticipantType { get; private set; }
    public string? Payload { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private ConversationAuditEntry()
    {
    }

    public ConversationAuditEntry(
        ConversationId conversationId,
        ConversationAuditAction action,
        ChatUserId? actorId,
        ParticipantType? actorType,
        ChatUserId? targetParticipantId = null,
        ParticipantType? targetParticipantType = null,
        string? payload = null,
        DateTime? occurredAt = null)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        Action = action;
        ActorId = actorId;
        ActorType = actorType;
        TargetParticipantId = targetParticipantId;
        TargetParticipantType = targetParticipantType;
        Payload = payload;
        OccurredAt = occurredAt ?? DateTime.UtcNow;
    }
}
