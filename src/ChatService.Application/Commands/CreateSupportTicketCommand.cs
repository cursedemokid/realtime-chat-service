using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record CreateSupportTicketCommand(
    ChatUserId UserId,
    ParticipantType InitiatorType,
    string Theme,
    long? AssignedManagerId,
    string? InitialMessage = null) : IRequest<SupportTicketDto>;
