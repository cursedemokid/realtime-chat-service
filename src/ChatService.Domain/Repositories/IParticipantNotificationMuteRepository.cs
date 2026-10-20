using ChatService.Domain.Entities;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Repositories;

public interface IParticipantNotificationMuteRepository
{
    Task<bool> IsMutedAsync(
        ConversationId conversationId,
        ChatUserId viewerId,
        ChatUserId mutedParticipantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ChatUserId>> GetMutedTargetsAsync(
        ConversationId conversationId,
        ChatUserId viewerId,
        CancellationToken ct = default);

    Task AddAsync(ParticipantNotificationMute mute, CancellationToken ct = default);

    Task RemoveAsync(
        ConversationId conversationId,
        ChatUserId viewerId,
        ChatUserId mutedParticipantId,
        CancellationToken ct = default);
}