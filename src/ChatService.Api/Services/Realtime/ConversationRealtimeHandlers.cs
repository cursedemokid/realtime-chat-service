using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Events;
using MediatR;

namespace ChatService.Api.Services.Realtime;

public sealed class ConversationParticipantRoleChangedHandler
    : INotificationHandler<ConversationParticipantRoleChangedEvent>
{
    private readonly IConversationRealtimeNotifier _notifier;

    public ConversationParticipantRoleChangedHandler(IConversationRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task Handle(ConversationParticipantRoleChangedEvent notification, CancellationToken cancellationToken)
        => _notifier.NotifyParticipantRoleChangedAsync(
            notification.ConversationId,
            notification.ParticipantId,
            notification.ParticipantType,
            notification.OldRole,
            notification.NewRole,
            cancellationToken);
}

public sealed class ConversationParticipantAddedHandler
    : INotificationHandler<ConversationParticipantAddedEvent>
{
    private readonly IConversationRealtimeNotifier _notifier;
    private readonly IMapper _mapper;

    public ConversationParticipantAddedHandler(IConversationRealtimeNotifier notifier, IMapper mapper)
    {
        _notifier = notifier;
        _mapper = mapper;
    }

    public Task Handle(ConversationParticipantAddedEvent notification, CancellationToken cancellationToken)
        => _notifier.NotifyParticipantAddedAsync(
            notification.ChatId,
            _mapper.Map<ParticipantDto>(notification.Participant),
            cancellationToken);
}

public sealed class ConversationParticipantRemovedHandler
    : INotificationHandler<ConversationParticipantRemovedEvent>
{
    private readonly IConversationRealtimeNotifier _notifier;

    public ConversationParticipantRemovedHandler(IConversationRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task Handle(ConversationParticipantRemovedEvent notification, CancellationToken cancellationToken)
        => _notifier.NotifyParticipantRemovedAsync(
            notification.ConversationId,
            notification.ParticipantId,
            notification.ParticipantType,
            cancellationToken);
}

public sealed class ConversationStatusChangedRealtimeHandler
    : INotificationHandler<ConversationStatusChangedEvent>
{
    private readonly IConversationRealtimeNotifier _notifier;

    public ConversationStatusChangedRealtimeHandler(IConversationRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task Handle(ConversationStatusChangedEvent notification, CancellationToken cancellationToken)
        => _notifier.NotifyConversationStatusChangedAsync(
            notification.ConversationId.Value,
            notification.NewStatus,
            cancellationToken);
}
