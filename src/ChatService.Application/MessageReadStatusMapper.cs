using ChatService.Application.Dto;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application;

public static class MessageReadStatusMapper
{
    public static int CountOtherActiveParticipants(
        Conversation conversation,
        ChatUserId viewerUserId)
        => conversation.Participants.Count(p =>
            p.IsActive && p.UserId != viewerUserId);

    public static MessageReadStatus Resolve(
        Message message,
        ChatUserId viewerUserId,
        ParticipantType viewerType,
        int otherActiveParticipantCount = 0)
    {
        bool isAuthor = message.Author.UserId == viewerUserId && message.Author.Type == viewerType;

        if (isAuthor)
        {
            bool othersRead = message.ReadReceipts.Any(r =>
                !(r.ReaderId == viewerUserId && r.ReaderType == viewerType));
            if (othersRead)
                return MessageReadStatus.Read;

            return otherActiveParticipantCount > 0
                ? MessageReadStatus.Delivered
                : MessageReadStatus.Sent;
        }

        bool viewerRead = message.ReadReceipts.Any(r =>
            r.ReaderId == viewerUserId && r.ReaderType == viewerType);
        return viewerRead ? MessageReadStatus.Read : MessageReadStatus.Sent;
    }

    public static void EnrichMessageDto(
        MessageDto dto,
        Message message,
        ChatUserId viewerUserId,
        ParticipantType viewerType,
        int otherActiveParticipantCount)
    {
        dto.IsAuthor = dto.Author.Id == viewerUserId;  
        dto.ReadStatus = Resolve(message, viewerUserId, viewerType, otherActiveParticipantCount);
    }

    public static void EnrichMessageDto(
        MessageDto dto,
        Message message,
        ChatUserId viewerUserId,
        ParticipantType viewerType)
    {
        EnrichMessageDto(dto, message, viewerUserId, viewerType, otherActiveParticipantCount: 0);
    }
}
