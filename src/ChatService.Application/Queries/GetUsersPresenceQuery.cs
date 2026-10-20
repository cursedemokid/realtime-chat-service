using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record PresenceLookup(ChatUserId UserId, ParticipantType Type);

public record GetUsersPresenceQuery(IEnumerable<PresenceLookup> Participants) : IRequest<IReadOnlyList<PresenceDto>>;
