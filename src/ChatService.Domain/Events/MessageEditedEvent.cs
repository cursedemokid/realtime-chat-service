using System;
using ChatService.Domain.Abstractions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record MessageEditedEvent(MessageId MessageId, ConversationId ConversationId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}