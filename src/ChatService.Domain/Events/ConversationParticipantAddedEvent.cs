using ChatService.Domain.Abstractions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record ConversationParticipantAddedEvent(
    Guid ChatId,
    ConversationParticipant Participant,
    bool IsRejoin) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
