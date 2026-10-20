using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(ConversationId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Conversation>> GetByParticipantAsync(
        ChatUserId userId, ParticipantType type, CancellationToken ct = default);
    Task<(IReadOnlyList<Conversation> Items, int TotalCount)> GetPageByParticipantAsync(
        ChatUserId userId, ParticipantType type, int skip, int take, CancellationToken ct = default);
    Task<(IReadOnlyList<Conversation> Items, int TotalCount)> SearchAsync(
        ChatUserId viewerUserId, ParticipantType viewerType, string query, int skip, int take, CancellationToken ct = default);

    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
}
