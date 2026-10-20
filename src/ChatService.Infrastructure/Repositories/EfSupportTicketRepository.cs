using ChatService.Domain.Aggregates;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatService.Domain.Enums;

namespace ChatService.Infrastructure.Repositories;

public class EfSupportTicketRepository : ISupportTicketRepository
{
    private readonly ConversationDbContext _context;

    public EfSupportTicketRepository(ConversationDbContext context)
    {
        _context = context;
    }

    public async Task<SupportTicket?> GetByIdAsync(SupportTicketId id, CancellationToken ct = default)
        => await _context.SupportTickets
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task AddAsync(SupportTicket ticket, CancellationToken ct = default)
        => _context.SupportTickets.AddAsync(ticket, ct).AsTask();

    public Task UpdateAsync(SupportTicket ticket, CancellationToken ct = default)
    {
        _context.SupportTickets.Update(ticket);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<SupportTicket> Items, int TotalCount)> GetPageAsync(
        ChatUserId viewerId,
        ParticipantType viewerType,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = _context.SupportTickets.AsNoTracking().Include(t => t.History).AsQueryable();
        if (viewerType != ParticipantType.Manager)
            query = query.Where(t => t.Initiator.UserId == viewerId);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.OpenedAt)
            .Skip(Math.Max(skip, 0)).Take(Math.Clamp(take, 1, 100)).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetThemesByConversationIdsAsync(
        IEnumerable<Guid> conversationIds,
        CancellationToken ct = default)
    {
        var ids = conversationIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var idSet = ids.Select(ConversationId.From).ToList();

        var rows = await _context.SupportTickets
            .AsNoTracking()
            .Where(t => t.ConversationId != null && idSet.Contains(t.ConversationId))
            .Select(t => new { t.ConversationId, t.Theme })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.ConversationId!.Value, x => x.Theme);
    }
}
