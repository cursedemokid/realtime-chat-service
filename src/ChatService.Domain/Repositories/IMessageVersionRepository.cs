using ChatService.Domain.Entities;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Repositories;

public interface IMessageVersionRepository
{
    Task AddAsync(MessageVersion version, CancellationToken ct = default);

    Task<int> GetLatestVersionNumberAsync(MessageId messageId, CancellationToken ct = default);

    Task<IReadOnlyList<MessageVersion>> GetByMessageIdAsync(
        MessageId messageId,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<int> CountByMessageIdAsync(MessageId messageId, CancellationToken ct = default);
}
