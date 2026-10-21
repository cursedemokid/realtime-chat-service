using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application;

public static class SupportTicketAuthorization
{
    public static bool CanView(SupportTicket ticket, ChatUserId viewer, ParticipantType type)
        => ticket.Initiator.UserId.TenantId == viewer.TenantId &&
           (type == ParticipantType.Manager ||
            ticket.Initiator.UserId == viewer && ticket.Initiator.Type == type);

    public static void EnsureCanManage(SupportTicket ticket, ChatUserId actor, ParticipantType type)
    {
        if (type != ParticipantType.Manager || ticket.Initiator.UserId.TenantId != actor.TenantId)
            throw new ForbiddenException("A manager from the ticket's organization is required.");
    }
}
