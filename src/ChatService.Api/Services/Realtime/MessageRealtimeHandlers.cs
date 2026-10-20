using AutoMapper;
using ChatService.Application;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Events;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Api.Services.Realtime;

public sealed class MessageSentRealtimeHandler : INotificationHandler<MessageSentEvent>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationRealtimeNotifier _notifier;
    private readonly IMapper _mapper;

    public MessageSentRealtimeHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IConversationRealtimeNotifier notifier,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _notifier = notifier;
        _mapper = mapper;
    }

    public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(notification.MessageId, cancellationToken);
        if (message == null)
            return;

        var conversation = await _conversationRepository.GetByIdAsync(notification.ConversationId, cancellationToken);
        var dto = _mapper.Map<MessageDto>(message);
        if (conversation != null)
            MessageReadStatusMapper.EnrichMessageDto(dto, message, message.Author.UserId, message.Author.Type);
        else
            MessageReadStatusMapper.EnrichMessageDto(dto, message, message.Author.UserId, message.Author.Type);

        await _notifier.NotifyMessageSentAsync(notification.ConversationId.Value, dto, cancellationToken);
    }
}

public sealed class InboxMessageSentHandler : INotificationHandler<MessageSentEvent>
{
    private readonly IInboxUpdateService _inboxUpdateService;

    public InboxMessageSentHandler(IInboxUpdateService inboxUpdateService)
    {
        _inboxUpdateService = inboxUpdateService;
    }

    public Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
        => _inboxUpdateService.NotifyConversationParticipantsAsync(
            notification.ConversationId.Value,
            cancellationToken);
}

public sealed class MessageEditedRealtimeHandler : INotificationHandler<MessageEditedEvent>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationRealtimeNotifier _notifier;
    private readonly IMapper _mapper;

    public MessageEditedRealtimeHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IConversationRealtimeNotifier notifier,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _notifier = notifier;
        _mapper = mapper;
    }

    public async Task Handle(MessageEditedEvent notification, CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(notification.MessageId, cancellationToken);
        if (message == null)
            return;

        var conversation = await _conversationRepository.GetByIdAsync(notification.ConversationId, cancellationToken);
        var dto = _mapper.Map<MessageDto>(message);
        if (conversation != null)
            MessageReadStatusMapper.EnrichMessageDto(dto, message, message.Author.UserId, message.Author.Type);
        else
            MessageReadStatusMapper.EnrichMessageDto(dto, message, message.Author.UserId, message.Author.Type);

        await _notifier.NotifyMessageEditedAsync(notification.ConversationId.Value, dto, cancellationToken);
    }
}

public sealed class MessageDeletedRealtimeHandler : INotificationHandler<MessageDeletedEvent>
{
    private readonly IConversationRealtimeNotifier _notifier;

    public MessageDeletedRealtimeHandler(IConversationRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task Handle(MessageDeletedEvent notification, CancellationToken cancellationToken)
        => _notifier.NotifyMessageDeletedAsync(
            notification.ConversationId.Value,
            notification.MessageId.Value,
            cancellationToken);
}

public sealed class MessageReadRealtimeHandler : INotificationHandler<MessageReadEvent>
{
    private readonly IConversationRealtimeNotifier _notifier;

    public MessageReadRealtimeHandler(IConversationRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task Handle(MessageReadEvent notification, CancellationToken cancellationToken)
        => _notifier.NotifyMessageReadAsync(
            notification.ConversationId.Value,
            notification.MessageId.Value,
            notification.ReaderId,
            notification.ReaderType,
            cancellationToken);
}
