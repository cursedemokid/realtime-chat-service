using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record ConversationParticipantRemovedEvent(
    Guid ConversationId,
    ChatUserId ParticipantId,
    ParticipantType ParticipantType,
    DateTime OccurredOn,
    ChatUserId? ActorId,
    ParticipantType? ActorType,
    bool IsSelfLeave
) : IDomainEvent;
