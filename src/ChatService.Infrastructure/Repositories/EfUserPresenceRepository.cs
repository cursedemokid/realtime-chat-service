using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories;

public class EfUserPresenceRepository : IUserPresenceRepository
{
    private readonly ConversationDbContext _context;

    public EfUserPresenceRepository(ConversationDbContext context)
    {
        _context = context;
    }

    public async Task<UserPresence?> GetAsync(ChatUserId participantId, CancellationToken ct = default)
        => await _context.UserPresences
            .FirstOrDefaultAsync(p => p.UserId == participantId, ct);

    public async Task<IReadOnlyList<UserPresence>> GetManyAsync(
        IEnumerable<ChatUserId> userIds,
        CancellationToken ct = default)
    {
        var idList = userIds.ToList();
        if (idList.Count == 0)
            return Array.Empty<UserPresence>();

        var presences = await _context.UserPresences
            .Where(p => idList.Contains(p.UserId))
            .ToListAsync(ct);

        return presences;
    }

    public async Task<IReadOnlyList<UserPresence>> GetAllOnlineAsync(CancellationToken ct = default)
        => await _context.UserPresences
            .Where(p => p.Status != PresenceStatus.Offline)
            .ToListAsync(ct);

    public async Task UpsertAsync(UserPresence presence, CancellationToken ct = default)
    {
        var existing = await GetAsync(presence.UserId, ct);
        if (existing == null)
            await _context.UserPresences.AddAsync(presence, ct);
        else
            _context.UserPresences.Update(presence);
    }
}
