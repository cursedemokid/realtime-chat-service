using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record GetConversationMessagesQuery(
    Guid ConversationId,
    ChatUserId ViewerId,
    ParticipantType ViewerType,
    int Skip = 0,
    int Take = 100) : IRequest<IReadOnlyList<MessageDto>>;
