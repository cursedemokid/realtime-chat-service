using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.ValueObjects;

public sealed class TicketParticipant
{
    public ChatUserId UserId { get; private set; } = null!;
    public ParticipantType Type { get; private set; }
    public string DisplayName { get; private set; } = "";
    public string? AvatarUrl { get; private set; }

    private TicketParticipant() { }

    public TicketParticipant(ChatUserId userId, ParticipantType type, string displayName, string? avatarUrl = null)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Type = type;
        DisplayName = displayName ?? "";
        AvatarUrl = avatarUrl;
    }

    public static TicketParticipant FromConversationParticipant(ConversationParticipant p)
        => new(p.UserId, p.Type, p.DisplayName, p.AvatarUrl);
}