using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface IConversationSummaryBuilder
{
    Task<IReadOnlyDictionary<Guid, string>> LoadSupportThemesAsync(
        IReadOnlyList<Conversation> conversations,
        CancellationToken ct = default);

    Task<ConversationSummaryDto> BuildAsync(
        Conversation conversation,
        ChatUserId viewerId,
        ParticipantType viewerType,
        string? themeOverride = null,
        bool enrichParticipantMutes = false,
        CancellationToken ct = default);
}

public sealed class ConversationSummaryBuilder : IConversationSummaryBuilder
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISupportTicketRepository _supportTicketRepository;
    private readonly IParticipantNotificationMuteRepository _muteRepository;
    private readonly IMapper _mapper;

    public ConversationSummaryBuilder(
        IMessageRepository messageRepository,
        ISupportTicketRepository supportTicketRepository,
        IParticipantNotificationMuteRepository muteRepository,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _supportTicketRepository = supportTicketRepository;
        _muteRepository = muteRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> LoadSupportThemesAsync(
        IReadOnlyList<Conversation> conversations,
        CancellationToken ct = default)
    {
        var supportIds = conversations
            .Where(c => c.Kind == ConversationKind.Support)
            .Select(c => c.Id.Value)
            .ToList();

        if (supportIds.Count == 0)
            return new Dictionary<Guid, string>();

        return await _supportTicketRepository.GetThemesByConversationIdsAsync(supportIds, ct);
    }

    public async Task<ConversationSummaryDto> BuildAsync(
    Conversation conversation,
    ChatUserId viewerUserId,
    ParticipantType viewerType,
    string? themeOverride = null,
    bool enrichParticipantMutes = false,
    CancellationToken ct = default)
    {
        var viewer = conversation.GetActiveParticipant(viewerUserId, viewerType);
        var unread = await _messageRepository.CountUnreadAsync(conversation.Id, viewerUserId, ct);
        var last = await _messageRepository.GetLastMessageAsync(conversation.Id, ct);

        string? title = !string.IsNullOrWhiteSpace(conversation.Title)
            ? conversation.Title
            : conversation.Kind switch
            {
                ConversationKind.Support => themeOverride,
                ConversationKind.Order => conversation.ContextReference,
                ConversationKind.Event => conversation.ContextReference,
                _ => null
            };

        var participants = _mapper.Map<List<ParticipantDto>>(conversation.Participants);
        if (enrichParticipantMutes)
        {
            await ParticipantMuteEnricher.EnrichWithViewerMutesAsync(
                participants, conversation.Id, viewerUserId, _muteRepository, ct);
        }

        MessageDto? lastMessage = null;
        if (last != null)
        {
            lastMessage = _mapper.Map<MessageDto>(last);
            MessageReadStatusMapper.EnrichMessageDto(
                lastMessage,
                last,
                viewerUserId,
                viewerType,
                MessageReadStatusMapper.CountOtherActiveParticipants(conversation, viewerUserId));
        }

        return new ConversationSummaryDto
        {
            Id = conversation.Id.Value,
            Kind = conversation.Kind,
            Status = conversation.Status,
            ContextReference = conversation.ContextReference,
            Title = title,
            Description = conversation.Description,
            AvatarUrl = conversation.AvatarUrl,
            Participants = participants,
            CreatedAt = conversation.CreatedAt,
            LastActivityAt = conversation.LastActivityAt,
            UnreadCount = unread,
            IsMuted = viewer?.IsMuted ?? false,
            IsWriteBlocked = viewer?.IsWriteBlocked ?? false,
            IsPinned = viewer?.IsPinned ?? false,
            LastMessage = lastMessage
        };
    }
}
