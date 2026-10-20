using System.ComponentModel.DataAnnotations.Schema;
using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.Events;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Aggregates;

public class SupportTicket : AggregateRoot<SupportTicketId>
{
    public ConversationId? ConversationId { get; private set; }
    public TicketParticipant Initiator { get; private set; } = null!;
    public string Theme { get; private set; } = null!;
    public TicketStatus Status { get; private set; }
    public ChatUserId? AssignedManagerId { get; private set; }

    [NotMapped]
    public TicketParticipant? AssignedManager =>
        AssignedManagerId is { } id
            ? new TicketParticipant(id, ParticipantType.Manager, "", null)
            : null;

    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public int EscalationLevel { get; private set; }
    public string? CloseReason { get; private set; }
    private readonly List<TicketHistoryEntry> _history = new();
    public IReadOnlyList<TicketHistoryEntry> History => _history.AsReadOnly();

    [NotMapped]
    public SLAInfo Sla { get; private set; } = SLAInfo.Empty();

    private SupportTicket()
    {
    }

    private bool IsTerminal => Status is TicketStatus.Closed or TicketStatus.Rejected;

    public static SupportTicket Create(TicketParticipant initiator, string theme)
    {
        if (initiator == null)
            throw new DomainException("Initiator is required");
        if (string.IsNullOrWhiteSpace(theme))
            throw new DomainException("Theme is required");
        if (initiator.Type is ParticipantType.Manager or ParticipantType.System)
            throw new DomainException("Support ticket cannot be created by manager or system");

        var ticket = new SupportTicket
        {
            Id = SupportTicketId.New(),
            Initiator = initiator,
            Theme = theme.Trim(),
            Status = TicketStatus.New,
            OpenedAt = DateTime.UtcNow
        };

        ticket.RaiseDomainEvent(new SupportTicketCreatedEvent(ticket.Id, initiator, ticket.Theme));
        return ticket;
    }

    public void LinkConversation(ConversationId conversationId)
    {
        if (ConversationId != null)
            throw new DomainException("Conversation is already linked to this ticket");
        ConversationId = conversationId;
    }

    public void AssignManager(TicketParticipant manager)
    {
        EnsureCanMutate();
        if (manager == null)
            throw new DomainException("Manager is required");
        if (Status is not (TicketStatus.New or TicketStatus.InProgress or TicketStatus.Escalated))
            throw new DomainException("Cannot assign manager in the current ticket state");

        AssignedManagerId = manager.UserId;
        if (Status == TicketStatus.New)
            Status = TicketStatus.InProgress;
        _history.Add(new TicketHistoryEntry(manager.UserId, "Assigned"));
    }

    public void InProgress()
    {
        EnsureCanMutate();
        if (Status == TicketStatus.New)
            Status = TicketStatus.InProgress;
    }

    public void Escalate()
    {
        EnsureCanMutate();
        if (Status is not (TicketStatus.InProgress or TicketStatus.Escalated))
            throw new DomainException("Cannot escalate ticket in the current state");

        EscalationLevel++;
        Status = TicketStatus.Escalated;
        _history.Add(new TicketHistoryEntry(null, $"Escalated to level {EscalationLevel}"));
    }

    public void Close(string reason)
    {
        if (Status == TicketStatus.Closed)
            return;
        if (Status == TicketStatus.Rejected)
            throw new DomainException("Cannot close a rejected ticket");
        if (Status is not (TicketStatus.New or TicketStatus.InProgress or TicketStatus.Escalated))
            throw new DomainException("Cannot close ticket in the current state");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Close reason is required");

        Status = TicketStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        CloseReason = reason.Trim();
        _history.Add(new TicketHistoryEntry(null, $"Closed: {CloseReason}"));
    }

    public void Reject(string reason)
    {
        if (Status == TicketStatus.Rejected)
            return;
        if (Status == TicketStatus.Closed)
            throw new DomainException("Cannot reject a closed ticket");
        if (Status is not (TicketStatus.New or TicketStatus.InProgress or TicketStatus.Escalated))
            throw new DomainException("Cannot reject ticket in the current state");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reject reason is required");

        Status = TicketStatus.Rejected;
        ClosedAt = DateTime.UtcNow;
        CloseReason = reason.Trim();
        _history.Add(new TicketHistoryEntry(null, $"Rejected: {CloseReason}"));
    }

    public void SetSla(SLAInfo sla) => Sla = sla ?? SLAInfo.Empty();

    private void EnsureCanMutate()
    {
        if (IsTerminal)
            throw new DomainException("Ticket is in a terminal state");
    }
}
