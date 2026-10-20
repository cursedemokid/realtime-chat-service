using ChatService.Api.Hubs;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Api.Services;

public class ConversationHubNotifier : IConversationRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ConversationHubNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyParticipantRoleChangedAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        ConversationRole oldRole,
        ConversationRole newRole,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync(
            "ParticipantRoleChanged",
            new { conversationId, participantId = participantId.UserId, participantType, oldRole, newRole },
            cancellationToken);

    public Task NotifyParticipantAddedAsync(
        Guid conversationId,
        ParticipantDto participant,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync(
            "ParticipantAdded",
            new { conversationId, participant },
            cancellationToken);

    public Task NotifyParticipantRemovedAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync(
            "ParticipantRemoved",
            new { conversationId, participantId = participantId.UserId, participantType },
            cancellationToken);

    public Task NotifyConversationStatusChangedAsync(
        Guid conversationId,
        ConversationStatus status,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync(
            "ConversationStatusChanged",
            conversationId,
            status,
            cancellationToken);

    public Task NotifyMessageSentAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync("MessageReceived", message, cancellationToken);

    public Task NotifyMessageEditedAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync("MessageEdited", message, cancellationToken);

    public Task NotifyMessageDeletedAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync(
            "MessageDeleted",
            new { conversationId, messageId },
            cancellationToken);

    public Task NotifyMessageReadAsync(
        Guid conversationId,
        Guid messageId,
        ChatUserId readerId,
        ParticipantType readerType,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId)).SendAsync(
            "MessageRead",
            new { conversationId, messageId, readerId = readerId.UserId, readerType },
            cancellationToken);

    public Task NotifyPresenceChangedAsync(PresenceDto presence, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"tenant:{presence.TenantId}").SendAsync("PresenceChanged", presence, cancellationToken);

    public Task NotifyUserTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        string displayName,
        string? excludeConnectionId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { conversationId, participantId = participantId.UserId, participantType, displayName };
        var clients = excludeConnectionId == null
            ? _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId))
            : _hubContext.Clients.GroupExcept(SignalRGroups.Conversation(conversationId), excludeConnectionId);
        return clients.SendAsync("UserTyping", payload, cancellationToken);
    }

    public Task NotifyUserStoppedTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        string? excludeConnectionId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { conversationId, participantId = participantId.UserId, participantType };
        var clients = excludeConnectionId == null
            ? _hubContext.Clients.Group(SignalRGroups.Conversation(conversationId))
            : _hubContext.Clients.GroupExcept(SignalRGroups.Conversation(conversationId), excludeConnectionId);
        return clients.SendAsync("UserStoppedTyping", payload, cancellationToken);
    }

    public Task NotifyConversationInboxUpdatedAsync(
        ChatUserId participantId,
        ParticipantType participantType,
        ConversationInboxUpdateDto update,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Inbox(participantId, participantType))
            .SendAsync("ConversationInboxUpdated", update, cancellationToken);

    public Task NotifyUnreadTotalUpdatedAsync(
        ChatUserId participantId,
        ParticipantType participantType,
        int totalUnread,
        CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(SignalRGroups.Inbox(participantId, participantType))
            .SendAsync("UnreadTotalUpdated", totalUnread, cancellationToken);
}
