using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record DeleteMessageCommand(
    Guid ConversationId,
    Guid MessageId,
    ChatUserId ActorId,
    ParticipantType ActorType) : IRequest;
