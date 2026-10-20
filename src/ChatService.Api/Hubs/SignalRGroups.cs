using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Api.Hubs;

public static class SignalRGroups
{
    public static string Conversation(Guid conversationId) => $"conversation_{conversationId}";

    public static string Inbox(ChatUserId participantId, ParticipantType participantType)
        => $"inbox_{(int)participantType}_{participantId}";
}
