using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.ValueObjects;

public sealed class MessageAuthor
{
    public ChatUserId UserId { get; private set; } = null!;
    public ParticipantType Type { get; private set; }
    public ConversationRole Role { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }

    private MessageAuthor() { }

    public MessageAuthor(
        ChatUserId userId,
        ParticipantType type,
        string displayName,
        string? avatarUrl = null,
        ConversationRole role = ConversationRole.Member)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Type = type;
        Role = role;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        AvatarUrl = avatarUrl;
    }

    public static MessageAuthor FromParticipant(ConversationParticipant participant)
        => new(participant.UserId, participant.Type, participant.DisplayName, participant.AvatarUrl, participant.Role);

    public static MessageAuthor System(string displayName = "System")
        => new(new ChatUserId("system", 0), ParticipantType.System, displayName);
}