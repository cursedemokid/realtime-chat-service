using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Repositories;

public interface IConversationAuditRepository
{
    Task AddAsync(ConversationAuditEntry entry, CancellationToken ct = default);

    Task<(IReadOnlyList<ConversationAuditEntry> Items, int TotalCount)> GetByConversationAsync(
        ConversationId conversationId,
        int skip,
        int take,
        bool ascending,
        CancellationToken ct = default);
}
