using System;
using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record ConversationStatusChangedEvent(
    ConversationId ConversationId,
    ConversationStatus OldStatus,
    ConversationStatus NewStatus) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}