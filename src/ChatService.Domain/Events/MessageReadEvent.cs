using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record MessageReadEvent(MessageId MessageId, ConversationId ConversationId, ChatUserId ReaderId, ParticipantType ReaderType) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}