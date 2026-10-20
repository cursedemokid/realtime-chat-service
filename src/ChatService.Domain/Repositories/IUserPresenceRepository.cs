using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Repositories;

public interface IUserPresenceRepository
{
    Task<UserPresence?> GetAsync(ChatUserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserPresence>> GetManyAsync(IEnumerable<ChatUserId> userIds, CancellationToken ct = default);
    Task<IReadOnlyList<UserPresence>> GetAllOnlineAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(UserPresence presence, CancellationToken cancellationToken = default);
}
