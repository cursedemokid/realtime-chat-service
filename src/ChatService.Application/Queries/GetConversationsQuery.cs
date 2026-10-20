using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record GetConversationsQuery(
    ChatUserId ParticipantId,
    ParticipantType ParticipantType,
    int Skip,
    int Take) : IRequest<ConversationsPageDto>;

public record GetConversationParticipantsQuery(
    Guid ConversationId,
    ChatUserId ChatUserId,
    ParticipantType ViewerType,
    int Skip = 0,
    int Take = 50,
    bool ActiveOnly = true) : IRequest<ConversationParticipantsPageDto>;
