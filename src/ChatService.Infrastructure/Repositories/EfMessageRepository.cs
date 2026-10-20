using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories;

public class EfMessageRepository : IMessageRepository
{
    private readonly ConversationDbContext _context;

    public EfMessageRepository(ConversationDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct = default)
        => await _context.Messages
            .Include(m => m.Content.Attachments)
            .Include(m => m.ReadReceipts)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<Message?> GetLastMessageAsync(ConversationId conversationId, CancellationToken ct = default)
        => await _context.Messages
            .Include(m => m.Content.Attachments)
            .Include(m => m.ReadReceipts)
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Message>> GetPageByConversationAsync(
        ConversationId conversationId,
        int skip,
        int take,
        CancellationToken ct = default)
        => await _context.Messages
            .Include(m => m.Content.Attachments)
            .Include(m => m.ReadReceipts)
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountByConversationAsync(ConversationId conversationId, CancellationToken ct = default)
        => _context.Messages
            .AsNoTracking()
            .CountAsync(m => m.ConversationId == conversationId && m.DeletedAt == null, ct);

    public Task<int> CountUnreadAsync(
        ConversationId conversationId,
        ChatUserId viewerId,
        CancellationToken ct = default)
        => _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .Where(m => !(m.Author.UserId == viewerId))
            .Where(m => !m.ReadReceipts.Any(r => r.ReaderId == viewerId))
            .CountAsync(ct);

    public Task<int> CountUnreadTotalAsync(ChatUserId viewerId, CancellationToken ct = default)
        => _context.Messages
            .AsNoTracking()
            .Where(m => m.DeletedAt == null)
            .Where(m => !(m.Author.UserId == viewerId))
            .Where(m => !m.ReadReceipts.Any(r => r.ReaderId == viewerId))
            .Where(m => _context.Conversations.Any(c =>
                c.Id == m.ConversationId &&
                c.Participants.Any(p =>
                    p.UserId == viewerId && p.LeftAt == null)))
            .CountAsync(ct);

    public async Task<IReadOnlyList<Message>> GetMessagesPendingReadReceiptAsync(
        ConversationId conversationId,
        ChatUserId readerId,
        CancellationToken ct = default)
        => await _context.Messages
            .Include(m => m.Content.Attachments)
            .Include(m => m.ReadReceipts)
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .Where(m => !(m.Author.UserId == readerId))
            .Where(m => !m.ReadReceipts.Any(r => r.ReaderId == readerId))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Message> Items, int TotalCount)> SearchInConversationAsync(
        ConversationId conversationId,
        string query,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var normalized = query.Trim();
        var baseQuery = _context.Messages
            .Include(m => m.Content.Attachments)
            .Include(m => m.ReadReceipts)
            .Where(m => m.ConversationId == conversationId && m.DeletedAt == null)
            .Where(m => m.Content.Text != null && EF.Functions.ILike(m.Content.Text, $"%{normalized}%"));

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery.OrderByDescending(m => m.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<Message> Items, int TotalCount)> SearchAsync(
        ChatUserId viewerId,
        ParticipantType viewerType,
        string query,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var normalized = query.Trim();
        var baseQuery = _context.Messages
            .Include(m => m.Content.Attachments)
            .Include(m => m.ReadReceipts)
            .Where(m => m.DeletedAt == null)
            .Where(m => m.Content.Text != null && EF.Functions.ILike(m.Content.Text, $"%{normalized}%"))
            .Where(m => _context.Conversations.Any(c =>
                c.Id == m.ConversationId &&
                c.Participants.Any(p =>
                    p.UserId == viewerId && p.LeftAt == null)));

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery.OrderByDescending(m => m.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public Task AddAsync(Message message, CancellationToken ct = default)
        => _context.Messages.AddAsync(message, ct).AsTask();

    public Task UpdateAsync(Message message, CancellationToken ct = default)
    {
        _context.Messages.Update(message);
        return Task.CompletedTask;
    }
}
