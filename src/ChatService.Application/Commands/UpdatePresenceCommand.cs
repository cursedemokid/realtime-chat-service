using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record UpdatePresenceCommand(
    ChatUserId ParticipantId,
    ParticipantType ParticipantType,
    PresenceStatus? Status,
    bool ClearManualStatus = false) : IRequest<PresenceDto>;
