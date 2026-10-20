using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record ConversationParticipantRoleChangedEvent(
    Guid ConversationId,
    ChatUserId ParticipantId,
    ParticipantType ParticipantType,
    ConversationRole OldRole,
    ConversationRole NewRole,
    DateTime OccurredOn
) : IDomainEvent;
