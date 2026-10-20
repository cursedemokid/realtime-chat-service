using ChatService.Domain.Aggregates;
using ChatService.Domain.ValueObjects;
using ChatService.Domain.Enums;

namespace ChatService.Domain.Repositories;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(SupportTicketId id, CancellationToken ct = default);

    Task AddAsync(SupportTicket ticket, CancellationToken ct = default);

    Task UpdateAsync(SupportTicket ticket, CancellationToken ct = default);

    Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> GetPageAsync(
        ChatUserId viewerId,
        ParticipantType viewerType,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, string>> GetThemesByConversationIdsAsync(
        IEnumerable<Guid> conversationIds,
        CancellationToken ct = default);
}
