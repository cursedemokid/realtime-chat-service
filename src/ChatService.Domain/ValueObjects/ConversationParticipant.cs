using ChatService.Domain.Enums;

namespace ChatService.Domain.ValueObjects;

public sealed class ConversationParticipant
{
    public ChatUserId UserId { get; private set; } = null!;
    public ParticipantType Type { get; private set; }  
    public ConversationRole Role { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }

    /// <summary>Личное отключение уведомлений по всему чату (только для владельца этой записи).</summary>
    public bool IsMuted { get; private set; }

    /// <summary>Модераторский запрет отправки сообщений в чат.</summary>
    public bool IsWriteBlocked { get; private set; }

    public bool IsPinned { get; private set; }

    private ConversationParticipant()
    {
    }

    public ConversationParticipant(
        ChatUserId userId,          
        ParticipantType type,
        string displayName,
        string? avatarUrl = null,
        ConversationRole role = ConversationRole.Member)
    {
        if (userId == null) throw new ArgumentNullException(nameof(userId));
        UserId = userId;
        Type = type;
        Role = role;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        AvatarUrl = avatarUrl;
        JoinedAt = DateTime.UtcNow;
    }

    public bool IsActive => LeftAt == null;

    public void Leave()
    {
        if (LeftAt != null)
            return;
        LeftAt = DateTime.UtcNow;
    }

    public void Reactivate(string displayName, string? avatarUrl, ConversationRole? role = null)
    {
        LeftAt = null;
        JoinedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName;
        AvatarUrl = avatarUrl ?? AvatarUrl;
        if (role.HasValue)
            Role = role.Value;
    }

    public void AssignRole(ConversationRole role) => Role = role;

    public void SetMuted(bool muted) => IsMuted = muted;

    public void SetWriteBlocked(bool blocked) => IsWriteBlocked = blocked;

    public void SetPinned(bool pinned) => IsPinned = pinned;

}
