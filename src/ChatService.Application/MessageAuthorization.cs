using ChatService.Application.Dto;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application;

internal static class MessageAuthorization
{
    public static void EnsureCanEdit(Conversation conversation, Message message, ChatUserId actorUserId, ParticipantType actorType)
    {
        if (message.Author.UserId != actorUserId || message.Author.Type != actorType)
            throw new ForbiddenException("Only the author can edit this message");

        ConversationAuthorization.EnsureHasPermission(
            conversation, actorUserId, actorType, ConversationPermission.EditOwnMessage);
    }

    public static void EnsureCanDelete(Conversation conversation, Message message, ChatUserId actorUserId, ParticipantType actorType)
    {
        var isAuthor = message.Author.UserId == actorUserId && message.Author.Type == actorType;
        if (isAuthor)
        {
            ConversationAuthorization.EnsureHasPermission(
                conversation, actorUserId, actorType, ConversationPermission.DeleteOwnMessage);
            return;
        }
        ConversationAuthorization.EnsureHasPermission(
            conversation, actorUserId, actorType, ConversationPermission.DeleteAnyMessage);
    }
}
