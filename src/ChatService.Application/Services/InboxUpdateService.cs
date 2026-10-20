using AutoMapper;
using ChatService.Application;
using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public sealed class InboxUpdateService : IInboxUpdateService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRealtimeNotifier _notifier;
    private readonly IMapper _mapper;

    public InboxUpdateService(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IConversationRealtimeNotifier notifier,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _notifier = notifier;
        _mapper = mapper;
    }

    public async Task NotifyConversationParticipantsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId.From(conversationId), cancellationToken);
        if (conversation == null) return;

        foreach (var participant in conversation.Participants.Where(p => p.IsActive))
        {
            await NotifyParticipantAsync(conversationId, participant.UserId, participant.Type, cancellationToken);
        }
    }

    public async Task NotifyParticipantAsync(
        Guid conversationId,
        ChatUserId participantUserId,
        ParticipantType participantType,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId.From(conversationId), cancellationToken);
        if (conversation == null) return;

        if (!conversation.IsParticipant(participantUserId, participantType))
            return;

        var unread = await _messageRepository.CountUnreadAsync(conversation.Id, participantUserId, cancellationToken);
        var totalUnread = await _messageRepository.CountUnreadTotalAsync(participantUserId, cancellationToken);
        var last = await _messageRepository.GetLastMessageAsync(conversation.Id, cancellationToken);

        MessageDto? lastDto = null;
        if (last != null)
        {
            lastDto = _mapper.Map<MessageDto>(last);
            MessageReadStatusMapper.EnrichMessageDto(
                lastDto, last, participantUserId, participantType);
        }

        await _notifier.NotifyConversationInboxUpdatedAsync(
            participantUserId,
            participantType,
            new ConversationInboxUpdateDto
            {
                ConversationId = conversationId,
                UnreadCount = unread,
                LastActivityAt = conversation.LastActivityAt,
                LastMessage = lastDto
            },
            cancellationToken);

        await _notifier.NotifyUnreadTotalUpdatedAsync(
            participantUserId, participantType, totalUnread, cancellationToken);
    }
}