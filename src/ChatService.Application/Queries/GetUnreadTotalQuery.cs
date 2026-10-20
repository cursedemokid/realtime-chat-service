using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record GetUnreadTotalQuery(ChatUserId ViewerId, ChatService.Domain.Enums.ParticipantType ViewerType) : IRequest<int>;
