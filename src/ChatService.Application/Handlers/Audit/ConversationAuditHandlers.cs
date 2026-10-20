using ChatService.Application.Services;
using ChatService.Domain.Enums;
using ChatService.Domain.Events;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers.Audit;

public sealed class ConversationParticipantAddedAuditHandler
    : INotificationHandler<ConversationParticipantAddedEvent>
{
    private readonly IConversationAuditService _auditService;

    public ConversationParticipantAddedAuditHandler(IConversationAuditService auditService)
    {
        _auditService = auditService;
    }

    public Task Handle(ConversationParticipantAddedEvent notification, CancellationToken cancellationToken)
    {
        var action = notification.IsRejoin
            ? ConversationAuditAction.ParticipantJoined
            : ConversationAuditAction.ParticipantAdded;

        return _auditService.AppendAsync(
            ConversationId.From(notification.ChatId),
            action,
            null,
            null,
            notification.Participant.UserId,
            notification.Participant.Type,
            new { displayName = notification.Participant.DisplayName, isRejoin = notification.IsRejoin },
            cancellationToken);
    }
}

public sealed class ConversationParticipantRemovedAuditHandler
    : INotificationHandler<ConversationParticipantRemovedEvent>
{
    private readonly IConversationAuditService _auditService;

    public ConversationParticipantRemovedAuditHandler(IConversationAuditService auditService)
    {
        _auditService = auditService;
    }

    public Task Handle(ConversationParticipantRemovedEvent notification, CancellationToken cancellationToken)
    {
        var action = notification.IsSelfLeave
            ? ConversationAuditAction.ParticipantLeft
            : ConversationAuditAction.ParticipantRemoved;

        return _auditService.AppendAsync(
            ConversationId.From(notification.ConversationId),
            action,
            notification.ActorId,
            notification.ActorType,
            notification.ParticipantId,
            notification.ParticipantType,
            new { isSelfLeave = notification.IsSelfLeave },
            cancellationToken);
    }
}

public sealed class ConversationParticipantRoleChangedAuditHandler
    : INotificationHandler<ConversationParticipantRoleChangedEvent>
{
    private readonly IConversationAuditService _auditService;

    public ConversationParticipantRoleChangedAuditHandler(IConversationAuditService auditService)
    {
        _auditService = auditService;
    }

    public Task Handle(ConversationParticipantRoleChangedEvent notification, CancellationToken cancellationToken)
        => _auditService.AppendAsync(
            ConversationId.From(notification.ConversationId),
            ConversationAuditAction.RoleChanged,
            null,
            null,
            notification.ParticipantId,
            notification.ParticipantType,
            new { oldRole = notification.OldRole.ToString(), newRole = notification.NewRole.ToString() },
            cancellationToken);
}

public sealed class ConversationStatusChangedAuditHandler
    : INotificationHandler<ConversationStatusChangedEvent>
{
    private readonly IConversationAuditService _auditService;

    public ConversationStatusChangedAuditHandler(IConversationAuditService auditService)
    {
        _auditService = auditService;
    }

    public Task Handle(ConversationStatusChangedEvent notification, CancellationToken cancellationToken)
        => _auditService.AppendAsync(
            notification.ConversationId,
            ConversationAuditAction.StatusChanged,
            null,
            null,
            payload: new { oldStatus = notification.OldStatus.ToString(), newStatus = notification.NewStatus.ToString() },
            ct: cancellationToken);
}

public sealed class ConversationMetadataChangedAuditHandler
    : INotificationHandler<ConversationMetadataChangedEvent>
{
    private readonly IConversationAuditService _auditService;

    public ConversationMetadataChangedAuditHandler(IConversationAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task Handle(ConversationMetadataChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.OldTitle != notification.NewTitle)
        {
            await _auditService.AppendAsync(
                notification.ConversationId,
                ConversationAuditAction.TitleChanged,
                notification.ActorId,
                notification.ActorType,
                payload: new { oldValue = notification.OldTitle, newValue = notification.NewTitle },
                ct: cancellationToken);
        }

        if (notification.OldDescription != notification.NewDescription)
        {
            await _auditService.AppendAsync(
                notification.ConversationId,
                ConversationAuditAction.DescriptionChanged,
                notification.ActorId,
                notification.ActorType,
                payload: new { oldValue = notification.OldDescription, newValue = notification.NewDescription },
                ct: cancellationToken);
        }

        if (notification.OldAvatarUrl != notification.NewAvatarUrl)
        {
            await _auditService.AppendAsync(
                notification.ConversationId,
                ConversationAuditAction.AvatarChanged,
                notification.ActorId,
                notification.ActorType,
                payload: new { oldValue = notification.OldAvatarUrl, newValue = notification.NewAvatarUrl },
                ct: cancellationToken);
        }
    }
}

public sealed class MessageDeletedAuditHandler
    : INotificationHandler<MessageDeletedEvent>
{
    private readonly IConversationAuditService _auditService;

    public MessageDeletedAuditHandler(IConversationAuditService auditService)
    {
        _auditService = auditService;
    }

    public Task Handle(MessageDeletedEvent notification, CancellationToken cancellationToken)
        => _auditService.AppendAsync(
            notification.ConversationId,
            ConversationAuditAction.MessageDeleted,
            notification.ActorId,
            notification.ActorType,
            payload: new { messageId = notification.MessageId.Value },
            ct: cancellationToken);
}
