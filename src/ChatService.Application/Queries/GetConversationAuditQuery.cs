using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record GetConversationAuditQuery(
    Guid ConversationId,
    ChatUserId ViewerId,
    ParticipantType ViewerType,
    int Skip = 0,
    int Take = 50,
    string Sort = "desc") : IRequest<ConversationAuditPageDto>;
