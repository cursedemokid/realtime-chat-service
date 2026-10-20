using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Application.Services;
using ChatService.Domain.Repositories;
using MediatR;

namespace ChatService.Application.Handlers;

public class GetConversationsHandler : IRequestHandler<GetConversationsQuery, ConversationsPageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationSummaryBuilder _summaryBuilder;

    public GetConversationsHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IConversationSummaryBuilder summaryBuilder)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _summaryBuilder = summaryBuilder;
    }

    public async Task<ConversationsPageDto> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var skip = Math.Max(request.Skip, 0);

        var (conversations, total) = await _conversationRepository.GetPageByParticipantAsync(
            request.ParticipantId, request.ParticipantType, skip, take, cancellationToken);

        var totalUnread = await _messageRepository.CountUnreadTotalAsync(
            request.ParticipantId, cancellationToken);

        var themes = await _summaryBuilder.LoadSupportThemesAsync(conversations, cancellationToken);

        var result = new List<ConversationSummaryDto>();
        foreach (var conversation in conversations)
        {
            themes.TryGetValue(conversation.Id.Value, out var theme);
            result.Add(await _summaryBuilder.BuildAsync(
                conversation,
                request.ParticipantId,          
                request.ParticipantType,
                theme,
                enrichParticipantMutes: false,
                cancellationToken));
        }

        return new ConversationsPageDto
        {
            TotalCount = total,
            Skip = skip,
            Take = take,
            Items = result,
            TotalUnreadCount = totalUnread
        };
    }
}
