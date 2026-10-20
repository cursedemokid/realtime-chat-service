using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Application.Services;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class ManageConversationHandler : IRequestHandler<ManageConversationCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IParticipantNotificationMuteRepository _muteRepository;
    private readonly IParticipantResolver _participantResolver;
    private readonly IUnitOfWork _unitOfWork;

    public ManageConversationHandler(
        IConversationRepository conversationRepository,
        IParticipantNotificationMuteRepository muteRepository,
        IParticipantResolver participantResolver,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _muteRepository = muteRepository;
        _participantResolver = participantResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ManageConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(
            ConversationId.From(request.ConversationId), cancellationToken);
        if (conversation == null)
            throw new NotFoundException(nameof(Conversation), request.ConversationId);

        var actorUserId = new ChatUserId(request.ActorUserId.TenantId, request.ActorUserId.UserId);
        var actorType = request.ActorType;

        switch (request.Action)
        {
            case ConversationAction.AddParticipant:
                {
                    var target = RequireTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    var info = await _participantResolver.ResolveAsync(targetUserId, target.Type, cancellationToken);
                    conversation.AddParticipant(
                        new ConversationParticipant(info.UserId, info.Type, info.DisplayName, info.AvatarUrl),
                        actorUserId, actorType);
                    break;
                }
            case ConversationAction.RemoveParticipant:
                {
                    var target = RequireTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    conversation.RemoveParticipant(targetUserId, target.Type, actorUserId, actorType);
                    break;
                }
            case ConversationAction.AssignRole:
                {
                    var target = RequireTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    if (request.NewRole is not { } newRole)
                        throw new DomainException("NewRole is required");
                    conversation.AssignRole(targetUserId, target.Type, newRole, actorUserId, actorType);
                    break;
                }
            case ConversationAction.Mute:
                conversation.MuteChat(actorUserId, actorType);
                break;
            case ConversationAction.Unmute:
                conversation.UnmuteChat(actorUserId, actorType);
                break;
            case ConversationAction.MuteParticipant:
                {
                    var target = RequireTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    EnsureNotSelf(actorUserId, targetUserId);
                    EnsureActiveParticipant(conversation, actorUserId, actorType);
                    EnsureActiveParticipant(conversation, targetUserId, target.Type);
                    var alreadyMuted = await _muteRepository.IsMutedAsync(
                        conversation.Id, actorUserId, targetUserId, cancellationToken);
                    if (!alreadyMuted)
                    {
                        var mute = new ParticipantNotificationMute(
                            conversation.Id, actorUserId, targetUserId);
                        await _muteRepository.AddAsync(mute, cancellationToken);
                    }
                    break;
                }
            case ConversationAction.UnmuteParticipant:
                {
                    var target = RequireTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    EnsureNotSelf(actorUserId, targetUserId);
                    await _muteRepository.RemoveAsync(
                        conversation.Id, actorUserId, targetUserId, cancellationToken);
                    break;
                }
            case ConversationAction.BlockWrite:
                {
                    var target = RequireTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    conversation.BlockWrite(targetUserId, target.Type, actorUserId, actorType);
                    break;
                }
            case ConversationAction.UnblockWrite:
                {
                    var target = RequireTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    conversation.UnblockWrite(targetUserId, target.Type, actorUserId, actorType);
                    break;
                }
            case ConversationAction.Pin:
                {
                    var target = ResolveOptionalTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    conversation.Pin(targetUserId, target.Type, actorUserId, actorType);
                    break;
                }
            case ConversationAction.Unpin:
                {
                    var target = ResolveOptionalTarget(request);
                    var targetUserId = new ChatUserId(target.TenantId, target.Id);
                    conversation.Unpin(targetUserId, target.Type, actorUserId, actorType);
                    break;
                }
            case ConversationAction.Archive:
                conversation.Archive(actorUserId, actorType);
                break;
            case ConversationAction.Restore:
                conversation.Restore(actorUserId, actorType);
                break;
            default:
                throw new DomainException($"Unsupported action {request.Action}");
        }

        await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesWithDomainEventsAsync(cancellationToken);
    }

    private static (string TenantId, long Id, ParticipantType Type) RequireTarget(ManageConversationCommand request)
    {
        if (request.TargetUserId is not { } id || request.TargetParticipantType is not { } type)
            throw new DomainException("Target participant is required");
        return (id.TenantId, id.UserId, type);
    }

    private static (string TenantId, long Id, ParticipantType Type) ResolveOptionalTarget(ManageConversationCommand request)
    {
        if (request.TargetUserId is { } id && request.TargetParticipantType is { } type)
            return (id.TenantId, id.UserId, type);
        return (request.ActorUserId.TenantId, request.ActorUserId.UserId, request.ActorType);
    }

    private static void EnsureNotSelf(ChatUserId actor, ChatUserId target)
    {
        if (actor == target)
            throw new DomainException("Cannot perform this action on yourself");
    }

    private static void EnsureActiveParticipant(Conversation conversation, ChatUserId userId, ParticipantType type)
    {
        if (conversation.GetActiveParticipant(userId, type) == null)
            throw new DomainException("Participant is not active in this conversation");
    }
}