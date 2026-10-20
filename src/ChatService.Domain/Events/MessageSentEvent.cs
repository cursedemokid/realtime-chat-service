using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record MessageSentEvent(
    MessageId MessageId,
    ConversationId ConversationId,
    MessageAuthor Author,
    MessageContent Content,
    MessageType Type) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
