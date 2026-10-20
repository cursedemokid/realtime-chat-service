using ChatService.Domain.Abstractions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record SupportTicketCreatedEvent(
    SupportTicketId TicketId,
    TicketParticipant Initiator,
    string Theme) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
