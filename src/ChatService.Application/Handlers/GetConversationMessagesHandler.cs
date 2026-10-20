using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using static ChatService.Application.ConversationAuthorization;

namespace ChatService.Application.Handlers;

public class GetConversationMessagesHandler : IRequestHandler<GetConversationMessagesQuery, IReadOnlyList<MessageDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMapper _mapper;

    public GetConversationMessagesHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<MessageDto>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId.From(request.ConversationId), cancellationToken);
        if (conversation == null) throw new NotFoundException(nameof(Conversation), request.ConversationId);

        EnsureCanRead(conversation, request.ViewerId, request.ViewerType);

        var messages = await _messageRepository.GetPageByConversationAsync(
            conversation.Id,
            request.Skip,
            request.Take,
            cancellationToken);

        var mapped = _mapper.Map<List<MessageDto>>(messages);
        var otherParticipantCount = MessageReadStatusMapper.CountOtherActiveParticipants(
            conversation, request.ViewerId);
        for (var index = 0; index < mapped.Count; index++)
        {
            MessageReadStatusMapper.EnrichMessageDto(
                mapped[index],
                messages[index],
                request.ViewerId,
                request.ViewerType,
                otherParticipantCount);
        }

        return mapped;
    }
}
