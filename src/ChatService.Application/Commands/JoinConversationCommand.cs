using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record JoinConversationCommand(Guid ConversationId, ChatUserId ParticipantId, ParticipantType ParticipantType) : IRequest;
