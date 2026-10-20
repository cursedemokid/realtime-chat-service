using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Entities;

public class UserPresence
{
    public ChatUserId UserId { get; private set; } = null!;
    public PresenceStatus Status { get; private set; }
    public int ConnectionCount { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public DateTime StatusUpdatedAt { get; private set; }
    public bool IsManualStatus { get; private set; }

    private UserPresence() { }

    public UserPresence(ChatUserId userId)
    {
        UserId = userId;
        Status = PresenceStatus.Offline;
        ConnectionCount = 0;
        LastActivityAt = DateTime.UtcNow;
        StatusUpdatedAt = LastActivityAt;
    }

    public void OnConnected()
    {
        ConnectionCount++;
        LastActivityAt = DateTime.UtcNow;
        if (!IsManualStatus || Status == PresenceStatus.Offline)
            Status = PresenceStatus.Online;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    public void OnDisconnected()
    {
        ConnectionCount = Math.Max(0, ConnectionCount - 1);
        if (ConnectionCount == 0)
        {
            LastSeenAt = DateTime.UtcNow;
            if (!IsManualStatus || Status != PresenceStatus.DoNotDisturb)
                Status = PresenceStatus.Offline;
        }
        StatusUpdatedAt = DateTime.UtcNow;
    }

    public void TouchActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        if (Status == PresenceStatus.Away && !IsManualStatus)
            Status = PresenceStatus.Online;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    public void SetManualStatus(PresenceStatus status)
    {
        if (status is PresenceStatus.Offline or PresenceStatus.Away)
            throw new InvalidOperationException("Manual status must be Online or DoNotDisturb");

        IsManualStatus = status == PresenceStatus.DoNotDisturb;
        Status = status;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    public void ClearManualStatus()
    {
        IsManualStatus = false;
        Status = ConnectionCount > 0 ? PresenceStatus.Online : PresenceStatus.Offline;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    public void MarkAway(TimeSpan idleThreshold)
    {
        if (ConnectionCount == 0 || IsManualStatus)
            return;

        if (DateTime.UtcNow - LastActivityAt >= idleThreshold && Status == PresenceStatus.Online)
        {
            Status = PresenceStatus.Away;
            StatusUpdatedAt = DateTime.UtcNow;
        }
    }
}
