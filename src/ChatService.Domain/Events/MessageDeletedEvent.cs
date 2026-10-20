using System;
using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record MessageDeletedEvent(
    MessageId MessageId,
    ConversationId ConversationId,
    ChatUserId ActorId,
    ParticipantType ActorType) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
