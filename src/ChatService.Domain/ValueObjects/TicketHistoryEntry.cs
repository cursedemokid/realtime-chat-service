namespace ChatService.Domain.ValueObjects;

public sealed class TicketHistoryEntry
{
    public ChatUserId? ActorId { get; private set; }
    public string Action { get; private set; } = null!;
    public DateTime OccurredAt { get; private set; }

    private TicketHistoryEntry()
    {
    }

    public TicketHistoryEntry(ChatUserId? actorId, string action)
    {
        ActorId = actorId;
        Action = action;
        OccurredAt = DateTime.UtcNow;
    }
}
