using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record UpdateConversationStatusCommand(
    Guid ConversationId,
    ConversationStatus NewStatus,
    ChatUserId ActorId,
    ParticipantType ActorType) : IRequest;
