using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories;

public class EfConversationRepository : IConversationRepository
{
    private readonly ConversationDbContext _context;

    public EfConversationRepository(ConversationDbContext context)
    {
        _context = context;
    }

    // Membership must be re-read after a concurrent revocation, not served from the tracker.
    public Task<Conversation?> GetByIdForAuthorizationAsync(ConversationId id, CancellationToken ct = default)
        => _context.Conversations.AsNoTracking().Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken ct = default)
       => await _context.Conversations.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Conversation>> GetByParticipantAsync(
        ChatUserId userId, ParticipantType type, CancellationToken ct = default)
        => await ParticipantQuery(userId, type).ToListAsync(ct);

    public async Task<(IReadOnlyList<Conversation> Items, int TotalCount)> GetPageByParticipantAsync(
       ChatUserId userId, ParticipantType type, int skip, int take, CancellationToken ct = default)
    {
        var query = ParticipantQuery(userId, type)
            .OrderByDescending(c => c.LastActivityAt ?? c.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Conversation> Items, int TotalCount)> SearchAsync(
        ChatUserId viewerUserId, ParticipantType viewerType, string query, int skip, int take, CancellationToken ct = default)
    {
        var normalized = query.Trim().ToLower();
        var baseQuery = ParticipantQuery(viewerUserId, viewerType)
            .Where(c =>
                (c.Title != null && EF.Functions.ILike(c.Title, $"%{normalized}%")) ||
                (c.ContextReference != null && EF.Functions.ILike(c.ContextReference, $"%{normalized}%")) ||
                c.Participants.Any(p =>
                    p.LeftAt == null && EF.Functions.ILike(p.DisplayName, $"%{normalized}%")));
        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .OrderByDescending(c => c.LastActivityAt ?? c.CreatedAt)
            .Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(Conversation conversation, CancellationToken ct = default)
        => _context.Conversations.AddAsync(conversation, ct).AsTask();

    public Task UpdateAsync(Conversation conversation, CancellationToken ct = default)
    {
        _context.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    private IQueryable<Conversation> ParticipantQuery(ChatUserId userId, ParticipantType type)
    {
        if (type == ParticipantType.Manager)
        {
            return _context.Conversations
                .Include(c => c.Participants)
                .Where(c => c.Kind == ConversationKind.Support ||
                            c.Participants.Any(p =>
                                p.UserId == userId &&     
                                p.Type == type && p.LeftAt == null));
        }
        return _context.Conversations
            .Include(c => c.Participants)
            .Where(c => c.Participants.Any(p =>
                p.UserId == userId &&      
                p.Type == type && p.LeftAt == null));
    }
}
