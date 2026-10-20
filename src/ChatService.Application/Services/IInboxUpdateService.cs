using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface IInboxUpdateService
{
    Task NotifyConversationParticipantsAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task NotifyParticipantAsync(
        Guid conversationId,
        ChatUserId participantUserId,          
        ParticipantType participantType,
        CancellationToken cancellationToken = default);
}