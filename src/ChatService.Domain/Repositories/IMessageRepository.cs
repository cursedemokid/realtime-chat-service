using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(MessageId id, CancellationToken cancellationToken = default);

    Task<Message?> GetLastMessageAsync(ConversationId conversationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Message>> GetPageByConversationAsync(
        ConversationId conversationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountByConversationAsync(ConversationId conversationId, CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(ConversationId conversationId, ChatUserId userId, CancellationToken ct);
    Task<int> CountUnreadTotalAsync(ChatUserId userId, CancellationToken ct);

    Task<(IReadOnlyList<Message> Items, int TotalCount)> SearchInConversationAsync(
        ConversationId conversationId,
        string query,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Message> Items, int TotalCount)> SearchAsync(
        ChatUserId viewerUserId, ParticipantType viewerType, string query, int skip, int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Message>> GetMessagesPendingReadReceiptAsync(
        ConversationId conversationId, ChatUserId readerId, CancellationToken ct);

    Task AddAsync(Message message, CancellationToken cancellationToken = default);

    Task UpdateAsync(Message message, CancellationToken cancellationToken = default);
}
