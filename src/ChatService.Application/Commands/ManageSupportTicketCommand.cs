using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;
namespace ChatService.Application.Commands;

public enum SupportTicketAction
{
    AssignManager,
    StartProgress,
    Escalate,
    Close,
    Reject
}

public record ManageSupportTicketCommand(
    Guid TicketId,
    SupportTicketAction Action,
    ChatUserId ActorId,
    ParticipantType ActorType,
    string? Reason = null) : IRequest<SupportTicketDto>;
