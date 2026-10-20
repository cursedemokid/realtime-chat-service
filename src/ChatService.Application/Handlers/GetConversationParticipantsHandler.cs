using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Application.Services;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using static ChatService.Application.ConversationAuthorization;

namespace ChatService.Application.Handlers;

public class GetConversationParticipantsHandler : IRequestHandler<GetConversationParticipantsQuery, ConversationParticipantsPageDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IParticipantNotificationMuteRepository _muteRepository;
    private readonly IPresenceService _presenceService;
    private readonly IMapper _mapper;

    public GetConversationParticipantsHandler(
        IConversationRepository conversationRepository,
        IParticipantNotificationMuteRepository muteRepository,
        IPresenceService presenceService,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _muteRepository = muteRepository;
        _presenceService = presenceService;
        _mapper = mapper;
    }

    public async Task<ConversationParticipantsPageDto> Handle(GetConversationParticipantsQuery request, CancellationToken ct)
    {
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId.From(request.ConversationId), ct);
        if (conversation == null) throw new NotFoundException(nameof(Conversation), request.ConversationId);

        var viewerUserId = new ChatUserId(request.ChatUserId.TenantId, request.ChatUserId.UserId);
        EnsureCanRead(conversation, viewerUserId, request.ViewerType);

        var participants = request.ActiveOnly
            ? conversation.Participants.Where(p => p.IsActive).ToList()
            : conversation.Participants.ToList();

        var total = participants.Count;
        var page = participants
            .OrderBy(p => p.JoinedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        var dtos = _mapper.Map<List<ParticipantDto>>(page);

        var participantsToLoad = page.Select(p => (p.UserId, p.Type)).Distinct();
        var presences = await _presenceService.GetManyAsync(participantsToLoad, ct);
        var presenceMap = presences.ToDictionary(p => (p.ParticipantId, p.ParticipantType));

        foreach (var dto in dtos)
        {
            if (presenceMap.TryGetValue((dto.Id, (ParticipantType?)dto.Type), out var presence))
            {
                dto.PresenceStatus = presence.Status;
                dto.LastSeenAt = presence.LastSeenAt;
            }
        }

        await ParticipantMuteEnricher.EnrichWithViewerMutesAsync(
            dtos, conversation.Id, viewerUserId, _muteRepository, ct);

        return new ConversationParticipantsPageDto
        {
            TotalCount = total,
            Skip = request.Skip,
            Take = request.Take,
            Items = dtos
        };
    }
}
