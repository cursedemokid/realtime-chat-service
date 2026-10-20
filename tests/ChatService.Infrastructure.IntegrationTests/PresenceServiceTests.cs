using ChatService.Application;
using ChatService.Application.Abstractions;
using ChatService.Application.Services;
using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public class PresenceServiceTests
{
    [Fact]
    public async Task First_connection_persists_online_presence()
    {
        var repository = new InMemoryPresenceRepository();
        var unitOfWork = new RecordingUnitOfWork();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IUserPresenceRepository>(repository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);

        using var provider = services.BuildServiceProvider();
        var presenceService = provider.GetRequiredService<IPresenceService>();
        var dto = await presenceService.OnConnectedAsync(
            new ChatUserId("tenant-a", 42),
            ParticipantType.Customer);

        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(PresenceStatus.Online, dto.Status);
        Assert.Equal(42, dto.ParticipantId);
        Assert.Equal(ParticipantType.Customer, dto.ParticipantType);
        Assert.NotNull(await repository.GetAsync(new ChatUserId("tenant-a", 42)));
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public Task<int> SaveChangesWithDomainEventsAsync(CancellationToken cancellationToken = default)
            => SaveChangesAsync(cancellationToken);
    }

    private sealed class InMemoryPresenceRepository : IUserPresenceRepository
    {
        private readonly Dictionary<ChatUserId, UserPresence> _items = new();

        public Task<UserPresence?> GetAsync(ChatUserId userId, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(userId));

        public Task<IReadOnlyList<UserPresence>> GetManyAsync(
            IEnumerable<ChatUserId> userIds,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<UserPresence>>(
                userIds.Where(_items.ContainsKey).Select(id => _items[id]).ToList());

        public Task<IReadOnlyList<UserPresence>> GetAllOnlineAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<UserPresence>>(
                _items.Values.Where(p => p.Status != PresenceStatus.Offline).ToList());

        public Task UpsertAsync(UserPresence presence, CancellationToken cancellationToken = default)
        {
            _items[presence.UserId] = presence;
            return Task.CompletedTask;
        }
    }
}
