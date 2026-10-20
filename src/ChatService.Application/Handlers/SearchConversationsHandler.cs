using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Application.Services;
using ChatService.Domain.Repositories;
using MediatR;

namespace ChatService.Application.Handlers;

public class SearchConversationsHandler : IRequestHandler<SearchConversationsQuery, SearchConversationsResultDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationSummaryBuilder _summaryBuilder;

    public SearchConversationsHandler(
        IConversationRepository conversationRepository,
        IConversationSummaryBuilder summaryBuilder)
    {
        _conversationRepository = conversationRepository;
        _summaryBuilder = summaryBuilder;
    }

    public async Task<SearchConversationsResultDto> Handle(SearchConversationsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _conversationRepository.SearchAsync(
            request.ViewerId,
            request.ViewerType,
            request.Query,
            request.Skip,
            request.Take,
            cancellationToken);

        var themes = await _summaryBuilder.LoadSupportThemesAsync(items, cancellationToken);

        var summaries = new List<ConversationSummaryDto>();
        foreach (var conversation in items)
        {
            themes.TryGetValue(conversation.Id.Value, out var theme);
            summaries.Add(await _summaryBuilder.BuildAsync(
                conversation,
                request.ViewerId,
                request.ViewerType,
                theme,
                enrichParticipantMutes: false,
                cancellationToken));
        }

        return new SearchConversationsResultDto
        {
            TotalCount = total,
            Skip = request.Skip,
            Take = request.Take,
            Items = summaries
        };
    }
}
