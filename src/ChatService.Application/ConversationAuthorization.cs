using ChatService.Domain;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application;

internal static class ConversationAuthorization
{
    public static void EnsureCanRead(Conversation conversation, ChatUserId viewerId, ParticipantType viewerType)
    {
        if (viewerType == ParticipantType.Manager && conversation.Kind == ConversationKind.Support)
            return;

        var participant = conversation.GetActiveParticipant(viewerId, viewerType);
        if (participant == null)
            throw new ForbiddenException("You are not a participant of this conversation");

        if (!ConversationRolePermissions.Has(participant.Role, ConversationPermission.Read))
            throw new ForbiddenException("You do not have read access to this conversation");
    }

    public static void EnsureHasPermission(
        Conversation conversation,
        ChatUserId actorId,
        ParticipantType actorType,
        ConversationPermission permission)
    {
        if (HasGlobalSupportAccess(conversation, actorType))
            return;

        if (!conversation.HasPermission(actorId, actorType, permission))
            throw new ForbiddenException($"Missing permission: {permission}");
    }

    public static ConversationParticipant RequireActiveParticipant(
        Conversation conversation,
        ChatUserId participantId,
        ParticipantType participantType)
    {
        var participant = conversation.GetActiveParticipant(participantId, participantType);
        if (participant == null)
            throw new ForbiddenException("You are not a participant of this conversation");
        return participant;
    }

    public static void EnsureCanViewMessageVersions(
        Conversation conversation,
        ChatUserId viewerId,
        ParticipantType viewerType)
    {
        if (viewerType != ParticipantType.Manager)
            throw new ForbiddenException("Message version history is available to managers only");

        EnsureCanRead(conversation, viewerId, viewerType);
    }

    private static bool HasGlobalSupportAccess(Conversation conversation, ParticipantType viewerType)
        => viewerType == ParticipantType.Manager && conversation.Kind == ConversationKind.Support;
}
