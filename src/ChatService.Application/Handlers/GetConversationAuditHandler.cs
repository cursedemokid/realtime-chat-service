using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class GetConversationAuditHandler : IRequestHandler<GetConversationAuditQuery, ConversationAuditPageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationAuditRepository _auditRepository;
    private readonly IMapper _mapper;

    public GetConversationAuditHandler(
        IConversationRepository conversationRepository,
        IConversationAuditRepository auditRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _auditRepository = auditRepository;
        _mapper = mapper;
    }

    public async Task<ConversationAuditPageDto> Handle(GetConversationAuditQuery request, CancellationToken cancellationToken)
    {
        var conversationId = ConversationId.From(request.ConversationId);
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation == null)
            throw new NotFoundException(nameof(Conversation), request.ConversationId);

        ConversationAuthorization.EnsureCanRead(conversation, request.ViewerId, request.ViewerType);

        var ascending = string.Equals(request.Sort, "asc", StringComparison.OrdinalIgnoreCase);
        var take = Math.Clamp(request.Take, 1, 200);
        var skip = Math.Max(request.Skip, 0);
        var (items, total) = await _auditRepository.GetByConversationAsync(
            conversationId, skip, take, ascending, cancellationToken);

        return new ConversationAuditPageDto
        {
            Items = _mapper.Map<List<ConversationAuditEntryDto>>(items),
            TotalCount = total,
            Skip = skip,
            Take = take
        };
    }
}
