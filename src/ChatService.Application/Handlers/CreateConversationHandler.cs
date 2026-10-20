using AutoMapper;
using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class CreateConversationHandler : IRequestHandler<CreateConversationCommand, ConversationSummaryDto>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IParticipantResolver _participantResolver;
    private readonly IUnitOfWorkExecutor _unitOfWork;
    private readonly IMapper _mapper;

    public CreateConversationHandler(
        IConversationRepository conversationRepository,
        IParticipantResolver participantResolver,
        IUnitOfWorkExecutor unitOfWork,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _participantResolver = participantResolver;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<ConversationSummaryDto> Handle(CreateConversationCommand request, CancellationToken cancellationToken) =>
        _unitOfWork.RunAsync(async ct =>
        {
            var conversation = await BuildConversationAsync(request, ct);
            await _conversationRepository.AddAsync(conversation, ct);

            return new ConversationSummaryDto
            {
                Id = conversation.Id.Value,
                Kind = conversation.Kind,
                Status = conversation.Status,
                Participants = _mapper.Map<List<ParticipantDto>>(conversation.Participants),
                CreatedAt = conversation.CreatedAt,
                LastActivityAt = conversation.LastActivityAt,
                UnreadCount = 0,
                LastMessage = null
            };
        }, cancellationToken);

    private async Task<Conversation> BuildConversationAsync(CreateConversationCommand request, CancellationToken ct)
    {
        var creatorUserId = new ChatUserId(request.TenantId, request.CreatorId);
        var creatorInfo = await _participantResolver.ResolveAsync(creatorUserId, request.CreatorType, ct);
        var creator = new ConversationParticipant(creatorInfo.UserId, creatorInfo.Type, creatorInfo.DisplayName, creatorInfo.AvatarUrl);

        var additional = request.AdditionalParticipants ?? Array.Empty<CreateConversationParticipantRequest>();
        var resolved = new List<ConversationParticipant> { creator };

        foreach (var p in additional)
        {
            var participantUserId = new ChatUserId(request.TenantId, p.Id);
            if (participantUserId == creatorUserId && p.Type == request.CreatorType)
                continue;

            var info = await _participantResolver.ResolveAsync(participantUserId, p.Type, ct);
            resolved.Add(new ConversationParticipant(info.UserId, info.Type, info.DisplayName, info.AvatarUrl, p.Role));
        }

        return request.Kind switch
        {
            ConversationKind.Direct when resolved.Count == 2 =>
                Conversation.CreateDirect(resolved[0], resolved[1]),
            ConversationKind.Order when resolved.Count == 2 =>
                Conversation.CreateOrderConversation(
                    request.ContextReference ?? throw new DomainException("Order id is required"),
                    resolved.First(p => p.Type == ParticipantType.Customer),
                    resolved.First(p => p.Type == ParticipantType.Executor)),
            ConversationKind.Group =>
                Conversation.CreateGroup(resolved),
            ConversationKind.Event =>
                Conversation.CreateEvent(request.ContextReference, resolved),
            ConversationKind.Support =>
                throw new DomainException("Use CreateSupportTicket for support conversations"),
            _ => throw new DomainException($"Cannot create conversation of kind {request.Kind} with {resolved.Count} participant(s)")
        };
    }
}