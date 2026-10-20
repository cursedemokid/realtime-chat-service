using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using ChatService.Api.Authentication;

namespace ChatService.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IPresenceService _presenceService;
    private readonly ITypingIndicatorService _typingIndicatorService;
    private readonly IConversationRealtimeNotifier _notifier;

    public ChatHub(
        IMediator mediator,
        IPresenceService presenceService,
        ITypingIndicatorService typingIndicatorService,
        IConversationRealtimeNotifier notifier)
    {
        _mediator = mediator;
        _presenceService = presenceService;
        _typingIndicatorService = typingIndicatorService;
        _notifier = notifier;
    }

    private Task<ChatUserId> GetCurrentUserIdAsync()
    {
        if (!ChatPrincipal.TryResolve(Context.User, out var userId, out _))
            throw new HubException("Unauthorized");
        return Task.FromResult(userId);
    }

    private ParticipantType GetCurrentUserType()
    {
        if (!ChatPrincipal.TryResolve(Context.User, out _, out var participantType))
            throw new HubException("Unknown role");
        return participantType;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{userId.TenantId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Inbox(userId, userType));
        var presence = await _presenceService.OnConnectedAsync(userId, userType);
        await Clients.Group($"tenant:{userId.TenantId}").SendAsync("PresenceChanged", presence);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var userType = GetCurrentUserType();
            var presence = await _presenceService.OnDisconnectedAsync(userId, userType);
            await Clients.Group($"tenant:{userId.TenantId}").SendAsync("PresenceChanged", presence);
        }
        catch (HubException)
        {
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(SendMessageInput input)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        var command = new SendMessageCommand(
            input.ConversationId,
            userId,
            userType,
            input.Text,
            input.Attachments,
            input.IsDateProposal);
        await _mediator.Send(command);
    }

    public async Task EditMessage(EditMessageInput input)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        await _mediator.Send(new EditMessageCommand(
            input.ConversationId,
            input.MessageId,
            userId,
            userType,
            input.Text,
            input.Attachments));
    }

    public async Task DeleteMessage(Guid conversationId, Guid messageId)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        await _mediator.Send(new DeleteMessageCommand(conversationId, messageId, userId, userType));
    }

    public async Task MarkMessagesRead(Guid conversationId)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        await _mediator.Send(new MarkMessagesReadCommand(conversationId, userId, userType));
    }

    public async Task UpdateConversationStatus(UpdateConversationStatusInput input)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        if (userType != ParticipantType.Manager && userType != ParticipantType.Customer)
            throw new HubException("Not allowed");
        await _mediator.Send(new UpdateConversationStatusCommand(input.ConversationId, input.NewStatus, userId, userType));
    }

    public async Task UpdatePresence(UpdatePresenceInput input)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        await _mediator.Send(new UpdatePresenceCommand(
            userId,
            userType,
            input.Status,
            input.ClearManualStatus));
    }

    public async Task Heartbeat()
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        await _presenceService.TouchActivityAsync(userId, userType);
    }

    public async Task StartTyping(Guid conversationId)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        var typingUser = await _typingIndicatorService.SetTypingAsync(conversationId, userId, userType);
        if (typingUser == null)
            return;

        await _notifier.NotifyUserTypingAsync(
            conversationId,
            userId,
            userType,
            typingUser.DisplayName,
            Context.ConnectionId);
    }

    public async Task StopTyping(Guid conversationId)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        var cleared = await _typingIndicatorService.ClearTypingAsync(conversationId, userId, userType);
        if (!cleared)
            return;

        await _notifier.NotifyUserStoppedTypingAsync(
            conversationId,
            userId,
            userType,
            Context.ConnectionId);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        await _mediator.Send(new JoinConversationCommand(conversationId, userId, userType));
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Conversation(conversationId));
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Conversation(conversationId));
    }

    public async Task ManageConversation(ManageConversationInput input)
    {
        var userId = await GetCurrentUserIdAsync();
        var userType = GetCurrentUserType();
        ChatUserId? targetUserId = null;
        if (input.TargetParticipantId.HasValue && input.TargetParticipantType.HasValue)
        {
            targetUserId = new ChatUserId(userId.TenantId, input.TargetParticipantId.Value);
        }

        await _mediator.Send(new ManageConversationCommand(
            input.ConversationId,
            input.Action,
            userId,
            userType,
            targetUserId,
            input.TargetParticipantType,
            input.NewRole));
    }
}

public record SendMessageInput(Guid ConversationId, string? Text, List<MessageAttachmentDto>? Attachments, bool IsDateProposal);
public record EditMessageInput(Guid ConversationId, Guid MessageId, string Text, List<MessageAttachmentDto>? Attachments = null);
public record UpdateConversationStatusInput(Guid ConversationId, ConversationStatus NewStatus);
public record UpdatePresenceInput(PresenceStatus? Status, bool ClearManualStatus = false);
public record ManageConversationInput(
    Guid ConversationId,
    ConversationAction Action,
    long? TargetParticipantId = null,
    ParticipantType? TargetParticipantType = null,
    ConversationRole? NewRole = null);
