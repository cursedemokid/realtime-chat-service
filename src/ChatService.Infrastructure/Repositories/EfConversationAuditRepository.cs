using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories;

public class EfConversationAuditRepository : IConversationAuditRepository
{
    private readonly ConversationDbContext _context;

    public EfConversationAuditRepository(ConversationDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(ConversationAuditEntry entry, CancellationToken ct = default)
        => _context.ConversationAuditEntries.AddAsync(entry, ct).AsTask();

    public async Task<(IReadOnlyList<ConversationAuditEntry> Items, int TotalCount)> GetByConversationAsync(
        ConversationId conversationId,
        int skip,
        int take,
        bool ascending,
        CancellationToken ct = default)
    {
        var query = _context.ConversationAuditEntries
            .AsNoTracking()
            .Where(e => e.ConversationId == conversationId);

        var total = await query.CountAsync(ct);
        query = ascending
            ? query.OrderBy(e => e.OccurredAt)
            : query.OrderByDescending(e => e.OccurredAt);

        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }
}
