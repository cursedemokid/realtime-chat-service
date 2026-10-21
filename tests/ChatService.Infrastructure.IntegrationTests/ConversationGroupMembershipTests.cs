using ChatService.Api.Hubs;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public class ConversationGroupMembershipTests
{
    private sealed class Groups : IGroupManager
    {
        public HashSet<(string Connection, string Group)> Members { get; } = new();
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
        { Members.Add((connectionId, groupName)); return Task.CompletedTask; }
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
        { Members.Remove((connectionId, groupName)); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Removal_revokes_all_connections_without_relying_on_the_client()
    {
        var groups = new Groups();
        var memberships = new ConversationGroupMembership(groups);
        var room = Guid.NewGuid();
        var user = new ChatUserId("tenant-a", 42);
        await memberships.JoinAsync(room, user, ParticipantType.Customer, "tab-1", () => Task.CompletedTask);
        await memberships.JoinAsync(room, user, ParticipantType.Customer, "tab-2", () => Task.CompletedTask);
        await memberships.JoinAsync(room, new("tenant-a", 43), ParticipantType.Customer, "other", () => Task.CompletedTask);
        await memberships.RevokeAsync(room, user, ParticipantType.Customer);
        Assert.Equal(("other", SignalRGroups.Conversation(room)), Assert.Single(groups.Members));
    }

    [Fact]
    public async Task Failed_authorization_never_subscribes_a_connection()
    {
        var groups = new Groups();
        var memberships = new ConversationGroupMembership(groups);
        await Assert.ThrowsAsync<InvalidOperationException>(() => memberships.JoinAsync(Guid.NewGuid(),
            new("tenant-a", 42), ParticipantType.Customer, "denied", () => throw new InvalidOperationException()));
        Assert.Empty(groups.Members);
    }

    [Fact]
    public async Task Removal_waits_for_an_inflight_authorization_and_revokes_its_subscription()
    {
        var groups = new Groups();
        var memberships = new ConversationGroupMembership(groups);
        var room = Guid.NewGuid(); var user = new ChatUserId("tenant-a", 42);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var checks = 0;
        var join = memberships.JoinAsync(room, user, ParticipantType.Customer, "racing", async () =>
        { if (++checks == 1) return; entered.SetResult(); await finish.Task; });
        await entered.Task;
        var removal = memberships.RevokeAsync(room, user, ParticipantType.Customer);
        finish.SetResult();
        await Task.WhenAll(join, removal);
        Assert.Empty(groups.Members);
    }
}
