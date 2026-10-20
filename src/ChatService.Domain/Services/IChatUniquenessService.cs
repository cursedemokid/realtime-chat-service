using ChatService.Domain.Enums;

namespace ChatService.Domain.Services;

public interface IChatUniquenessService
{
    Task<bool> IsDuplicateDirectChatAsync(long participant1Id, long participant2Id, CancellationToken cancellationToken = default);
    Task<bool> IsDuplicateSupportTicketAsync(long initiatorId, ParticipantType initiatorType, string? theme, CancellationToken cancellationToken = default);
    Task<bool> IsDuplicateEventChatAsync(Guid eventId, long bloggerId, CancellationToken cancellationToken = default);
}