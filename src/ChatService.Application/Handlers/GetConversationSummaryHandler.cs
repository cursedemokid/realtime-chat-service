using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Application.Services;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using static ChatService.Application.ConversationAuthorization;

namespace ChatService.Application.Handlers;

public class GetConversationSummaryHandler : IRequestHandler<GetConversationSummaryQuery, ConversationSummaryDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationSummaryBuilder _summaryBuilder;

    public GetConversationSummaryHandler(
        IConversationRepository conversationRepository,
        IConversationSummaryBuilder summaryBuilder)
    {
        _conversationRepository = conversationRepository;
        _summaryBuilder = summaryBuilder;
    }

    public async Task<ConversationSummaryDto> Handle(GetConversationSummaryQuery request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId.From(request.ConversationId), cancellationToken);
        if (conversation == null) throw new NotFoundException(nameof(Conversation), request.ConversationId);

        EnsureCanRead(conversation, request.ViewerId, request.ViewerType);

        string? theme = null;
        if (conversation.Kind == Domain.Enums.ConversationKind.Support)
        {
            var themes = await _summaryBuilder.LoadSupportThemesAsync([conversation], cancellationToken);
            themes.TryGetValue(conversation.Id.Value, out theme);
        }

        return await _summaryBuilder.BuildAsync(
            conversation,
            request.ViewerId,
            request.ViewerType,
            theme,
            enrichParticipantMutes: true,
            cancellationToken);
    }
}
