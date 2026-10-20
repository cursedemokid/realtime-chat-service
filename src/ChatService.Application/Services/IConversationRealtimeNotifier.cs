using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface IConversationRealtimeNotifier
{
    Task NotifyParticipantRoleChangedAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        ConversationRole oldRole,
        ConversationRole newRole,
        CancellationToken cancellationToken = default);

    Task NotifyParticipantAddedAsync(
        Guid conversationId,
        ParticipantDto participant,
        CancellationToken cancellationToken = default);

    Task NotifyParticipantRemovedAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        CancellationToken cancellationToken = default);

    Task NotifyConversationStatusChangedAsync(
        Guid conversationId,
        ConversationStatus status,
        CancellationToken cancellationToken = default);

    Task NotifyMessageSentAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default);

    Task NotifyMessageEditedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default);

    Task NotifyMessageDeletedAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);

    Task NotifyMessageReadAsync(
        Guid conversationId,
        Guid messageId,
        ChatUserId readerId,
        ParticipantType readerType,
        CancellationToken cancellationToken = default);

    Task NotifyPresenceChangedAsync(PresenceDto presence, CancellationToken cancellationToken = default);

    Task NotifyUserTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        string displayName,
        string? excludeConnectionId = null,
        CancellationToken cancellationToken = default);

    Task NotifyUserStoppedTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        string? excludeConnectionId = null,
        CancellationToken cancellationToken = default);

    Task NotifyConversationInboxUpdatedAsync(
        ChatUserId participantId,
        ParticipantType participantType,
        ConversationInboxUpdateDto update,
        CancellationToken cancellationToken = default);

    Task NotifyUnreadTotalUpdatedAsync(
        ChatUserId participantId,
        ParticipantType participantType,
        int totalUnread,
        CancellationToken cancellationToken = default);
}
