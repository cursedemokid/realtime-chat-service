using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Commands;

public record UpdateConversationMetadataCommand(
    Guid ConversationId,
    ChatUserId ActorId,
    ParticipantType ActorType,
    string? Title,
    string? Description,
    string? AvatarUrl) : MediatR.IRequest;
