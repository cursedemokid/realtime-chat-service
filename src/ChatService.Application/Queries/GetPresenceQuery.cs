using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record GetPresenceQuery(ChatUserId ParticipantId, ParticipantType ParticipantType) : IRequest<PresenceDto>;
