using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record SearchConversationsQuery(
    ChatUserId ViewerId,
    ParticipantType ViewerType,
    string Query,
    int Skip = 0,
    int Take = 20) : IRequest<SearchConversationsResultDto>;
