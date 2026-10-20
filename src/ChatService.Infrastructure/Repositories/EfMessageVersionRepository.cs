using ChatService.Domain.Entities;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories;

public class EfMessageVersionRepository : IMessageVersionRepository
{
    private readonly ConversationDbContext _context;

    public EfMessageVersionRepository(ConversationDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(MessageVersion version, CancellationToken ct = default)
        => _context.MessageVersions.AddAsync(version, ct).AsTask();

    public async Task<int> GetLatestVersionNumberAsync(MessageId messageId, CancellationToken ct = default)
    {
        var max = await _context.MessageVersions
            .Where(v => v.MessageId == messageId)
            .MaxAsync(v => (int?)v.Version, ct);
        return max ?? 0;
    }

    public async Task<IReadOnlyList<MessageVersion>> GetByMessageIdAsync(
        MessageId messageId,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        return await _context.MessageVersions
            .AsNoTracking()
            .Include(v => v.Attachments)
            .Where(v => v.MessageId == messageId)
            .OrderBy(v => v.Version)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<int> CountByMessageIdAsync(MessageId messageId, CancellationToken ct = default)
        => _context.MessageVersions.CountAsync(v => v.MessageId == messageId, ct);
}
