using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Entities;

public class ParticipantNotificationMute
{
    public ConversationId ConversationId { get; private set; } = null!;
    public ChatUserId ViewerId { get; private set; } = null!;
    public ChatUserId MutedParticipantId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private ParticipantNotificationMute() { }

    public ParticipantNotificationMute(
        ConversationId conversationId,
        ChatUserId viewerId,
        ChatUserId mutedParticipantId)
    {
        if (viewerId.UserId <= 0 || mutedParticipantId.UserId <= 0)
            throw new ArgumentOutOfRangeException();
        if (viewerId == mutedParticipantId)
            throw new ArgumentException("Cannot mute yourself");

        ConversationId = conversationId;
        ViewerId = viewerId;
        MutedParticipantId = mutedParticipantId;
        CreatedAt = DateTime.UtcNow;
    }
}