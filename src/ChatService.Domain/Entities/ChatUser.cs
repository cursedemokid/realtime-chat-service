using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Entities;

public class ChatUser
{
    public ChatUserId Id { get; private set; } = null!;
    public ParticipantType Type { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ChatUser() { }

    public ChatUser(ChatUserId id, ParticipantType type, string displayName, string? avatarUrl = null)
    {
        Id = id;
        Type = type;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        AvatarUrl = avatarUrl;
        CreatedAt = DateTime.UtcNow;
    }
}