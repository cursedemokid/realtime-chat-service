using System.Collections.Concurrent;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace ChatService.Api.Services;

public class InMemoryTypingIndicatorService : ITypingIndicatorService
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(8);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<TypingKey, TypingEntry> _entries = new();

    public InMemoryTypingIndicatorService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TypingUserDto?> SetTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        CancellationToken cancellationToken = default)
    {
        var participant = await GetActiveParticipantAsync(conversationId, participantId, participantType, cancellationToken);
        if (participant == null)
            return null;

        var key = new TypingKey(conversationId, participantId, participantType);
        var entry = new TypingEntry(participant.DisplayName, DateTime.UtcNow.Add(Ttl));
        _entries[key] = entry;

        return new TypingUserDto
        {
            ParticipantId = participantId.UserId,
            ParticipantType = participantType,
            DisplayName = participant.DisplayName
        };
    }

    public async Task<bool> ClearTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        CancellationToken cancellationToken = default)
    {
        var participant = await GetActiveParticipantAsync(conversationId, participantId, participantType, cancellationToken);
        if (participant == null)
            return false;

        return _entries.TryRemove(new TypingKey(conversationId, participantId, participantType), out _);
    }

    public Task<IReadOnlyList<TypingUserDto>> GetTypingUsersAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        PurgeExpired();
        var users = _entries
            .Where(kv => kv.Key.ConversationId == conversationId && kv.Value.ExpiresAt > DateTime.UtcNow)
            .Select(kv => new TypingUserDto
            {
                ParticipantId = kv.Key.ParticipantId.UserId,
                ParticipantType = kv.Key.ParticipantType,
                DisplayName = kv.Value.DisplayName
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<TypingUserDto>>(users);
    }

    private async Task<ConversationParticipant?> GetActiveParticipantAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var conversationRepository = scope.ServiceProvider.GetRequiredService<IConversationRepository>();

        var conversation = await conversationRepository.GetByIdAsync(ConversationId.From(conversationId), cancellationToken);
        if (conversation == null)
            throw new NotFoundException(nameof(conversation), conversationId);

        return conversation.GetActiveParticipant(participantId, participantType);
    }

    private void PurgeExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _entries.Where(kv => kv.Value.ExpiresAt <= now).Select(kv => kv.Key).ToList())
            _entries.TryRemove(key, out _);
    }

    private readonly record struct TypingKey(Guid ConversationId, ChatUserId ParticipantId, ParticipantType ParticipantType);

    private sealed record TypingEntry(string DisplayName, DateTime ExpiresAt);
}
