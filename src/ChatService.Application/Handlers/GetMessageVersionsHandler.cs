using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class GetMessageVersionsHandler : IRequestHandler<GetMessageVersionsQuery, MessageVersionsPageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageVersionRepository _messageVersionRepository;
    private readonly IMapper _mapper;

    public GetMessageVersionsHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMessageVersionRepository messageVersionRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _messageVersionRepository = messageVersionRepository;
        _mapper = mapper;
    }

    public async Task<MessageVersionsPageDto> Handle(GetMessageVersionsQuery request, CancellationToken cancellationToken)
    {
        var conversationId = ConversationId.From(request.ConversationId);
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
            throw new NotFoundException(nameof(Conversation), request.ConversationId);

        ConversationAuthorization.EnsureCanViewMessageVersions(
            conversation, request.ViewerId, request.ViewerType);

        var messageId = MessageId.From(request.MessageId);
        var message = await _messageRepository.GetByIdAsync(messageId, cancellationToken);
        if (message == null || message.ConversationId != conversationId)
            throw new NotFoundException(nameof(Message), request.MessageId);

        var total = await _messageVersionRepository.CountByMessageIdAsync(messageId, cancellationToken);
        var items = await _messageVersionRepository.GetByMessageIdAsync(
            messageId, request.Skip, request.Take, cancellationToken);

        return new MessageVersionsPageDto
        {
            Items = _mapper.Map<List<MessageVersionDto>>(items),
            TotalCount = total
        };
    }
}
