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

public class SearchMessagesHandler : IRequestHandler<SearchMessagesQuery, SearchMessagesResultDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMapper _mapper;

    public SearchMessagesHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _mapper = mapper;
    }

    public async Task<SearchMessagesResultDto> Handle(SearchMessagesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Message> items;
        int total;

        if (request.ConversationId.HasValue)
        {
            var conversation = await _conversationRepository.GetByIdAsync(
                ConversationId.From(request.ConversationId.Value), cancellationToken);
            if (conversation == null)
                throw new NotFoundException(nameof(Conversation), request.ConversationId.Value);

            EnsureCanRead(conversation, request.ViewerId, request.ViewerType);

            (items, total) = await _messageRepository.SearchInConversationAsync(
                conversation.Id,
                request.Query,
                request.Skip,
                request.Take,
                cancellationToken);

            var dtos = _mapper.Map<List<MessageDto>>(items);
            var otherCount = MessageReadStatusMapper.CountOtherActiveParticipants(
                conversation, request.ViewerId);
            for (var i = 0; i < items.Count; i++)
                MessageReadStatusMapper.EnrichMessageDto(dtos[i], items[i], request.ViewerId, request.ViewerType, otherCount);

            return new SearchMessagesResultDto
            {
                TotalCount = total,
                Skip = request.Skip,
                Take = request.Take,
                Items = dtos
            };
        }

        {
            (items, total) = await _messageRepository.SearchAsync(
                request.ViewerId,
                request.ViewerType,
                request.Query,
                request.Skip,
                request.Take,
                cancellationToken);
        }

        var mappedDtos = _mapper.Map<List<MessageDto>>(items);
        for (var i = 0; i < items.Count; i++)
            MessageReadStatusMapper.EnrichMessageDto(mappedDtos[i], items[i], request.ViewerId, request.ViewerType);

        return new SearchMessagesResultDto
        {
            TotalCount = total,
            Skip = request.Skip,
            Take = request.Take,
            Items = mappedDtos
        };
    }
}
