using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record GetConversationSummaryQuery(
    Guid ConversationId,
    ChatUserId ViewerId,
    ParticipantType ViewerType) : IRequest<ConversationSummaryDto>;
