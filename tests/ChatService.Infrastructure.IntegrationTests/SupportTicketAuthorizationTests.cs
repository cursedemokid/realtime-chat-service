using ChatService.Application;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public class SupportTicketAuthorizationTests
{
    private static SupportTicket Ticket() => SupportTicket.Create(
        new TicketParticipant(new ChatUserId("tenant-a", 42), ParticipantType.Customer, "Customer"), "Private");

    [Fact]
    public void Manager_from_another_tenant_cannot_read_or_manage_ticket()
    {
        var ticket = Ticket();
        var outsider = new ChatUserId("tenant-b", 42);
        Assert.False(SupportTicketAuthorization.CanView(ticket, outsider, ParticipantType.Manager));
        Assert.Throws<ForbiddenException>(() => SupportTicketAuthorization.EnsureCanManage(ticket, outsider, ParticipantType.Manager));
    }

    [Fact]
    public void Only_the_owner_role_or_a_manager_in_the_same_tenant_can_view()
    {
        var ticket = Ticket();
        Assert.True(SupportTicketAuthorization.CanView(ticket, new("tenant-a", 42), ParticipantType.Customer));
        Assert.False(SupportTicketAuthorization.CanView(ticket, new("tenant-a", 42), ParticipantType.Executor));
        Assert.False(SupportTicketAuthorization.CanView(ticket, new("tenant-a", 43), ParticipantType.Customer));
        Assert.True(SupportTicketAuthorization.CanView(ticket, new("tenant-a", 99), ParticipantType.Manager));
    }
}
