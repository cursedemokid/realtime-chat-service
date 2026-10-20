using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Events;

public sealed record ConversationMetadataChangedEvent(
    ConversationId ConversationId,
    ChatUserId ActorId,
    ParticipantType ActorType,
    string? OldTitle,
    string? NewTitle,
    string? OldDescription,
    string? NewDescription,
    string? OldAvatarUrl,
    string? NewAvatarUrl) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
