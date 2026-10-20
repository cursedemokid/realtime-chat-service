using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Repositories;

public class EfParticipantNotificationMuteRepository : IParticipantNotificationMuteRepository
{
    private readonly ConversationDbContext _context;

    public EfParticipantNotificationMuteRepository(ConversationDbContext context)
    {
        _context = context;
    }

    public Task<bool> IsMutedAsync(
        ConversationId conversationId,
        ChatUserId viewerId,
        ChatUserId mutedParticipantId,
        CancellationToken ct = default)
        => _context.ParticipantNotificationMutes
            .AsNoTracking()
            .AnyAsync(
                m => m.ConversationId == conversationId
                     && m.ViewerId == viewerId
                     && m.MutedParticipantId == mutedParticipantId,
                ct);

    public async Task<IReadOnlyList<ChatUserId>> GetMutedTargetsAsync(
        ConversationId conversationId,
        ChatUserId viewerId,
        CancellationToken ct = default)
    {
        var targets = await _context.ParticipantNotificationMutes
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.ViewerId == viewerId)
            .Select(m => m.MutedParticipantId)
            .ToListAsync(ct);

        return targets.AsReadOnly();
    }

    public Task AddAsync(ParticipantNotificationMute mute, CancellationToken ct = default)
        => _context.ParticipantNotificationMutes.AddAsync(mute, ct).AsTask();

    public async Task RemoveAsync(
        ConversationId conversationId,
        ChatUserId viewerId,
        ChatUserId mutedParticipantId,
        CancellationToken ct = default)
    {
        var entity = await _context.ParticipantNotificationMutes
            .FirstOrDefaultAsync(
                m => m.ConversationId == conversationId
                     && m.ViewerId == viewerId
                     && m.MutedParticipantId == mutedParticipantId,
                ct);

        if (entity != null)
            _context.ParticipantNotificationMutes.Remove(entity);
    }
}
