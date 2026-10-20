namespace ChatService.Application.Services;

public class NotificationRequestedMessage
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "ChatService";
    public string UserId { get; set; } = null!;
    public string NotificationType { get; set; } = null!;
    public int Priority { get; set; } = 2;
    public string Payload { get; set; } = "{}";
    public int[]? PreferredChannels { get; set; }
}
