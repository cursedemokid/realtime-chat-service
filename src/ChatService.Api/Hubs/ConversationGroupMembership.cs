using System.Collections.Concurrent;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Api.Hubs;

// Serialize authorization + subscription with revocation for each conversation.
// Group membership belongs to the server; a removed client need not cooperate.
public sealed class ConversationGroupMembership(IGroupManager groups)
{
    private sealed class Room
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public Dictionary<string, (ChatUserId User, ParticipantType Type)> Connections { get; } = new();
    }

    private readonly ConcurrentDictionary<Guid, Room> _rooms = new();

    public async Task JoinAsync(Guid conversation, ChatUserId user, ParticipantType type,
        string connection, Func<Task> authorize, CancellationToken ct = default)
    {
        // Reject arbitrary room IDs before retaining any per-room state.
        await authorize();
        var room = _rooms.GetOrAdd(conversation, _ => new Room());
        await room.Gate.WaitAsync(ct);
        try
        {
            await authorize();
            await groups.AddToGroupAsync(connection, SignalRGroups.Conversation(conversation), ct);
            room.Connections[connection] = (user, type);
        }
        finally { room.Gate.Release(); }
    }

    public async Task RevokeAsync(Guid conversation, ChatUserId user, ParticipantType type,
        CancellationToken ct = default)
    {
        if (!_rooms.TryGetValue(conversation, out var room)) return;
        await room.Gate.WaitAsync(ct);
        try
        {
            var connections = room.Connections.Where(x => x.Value.User == user && x.Value.Type == type)
                .Select(x => x.Key).ToArray();
            foreach (var connection in connections)
            {
                await groups.RemoveFromGroupAsync(connection, SignalRGroups.Conversation(conversation), ct);
                room.Connections.Remove(connection);
            }
        }
        finally { room.Gate.Release(); }
    }

    public async Task LeaveAsync(Guid conversation, string connection, CancellationToken ct = default)
    {
        if (!_rooms.TryGetValue(conversation, out var room)) return;
        await room.Gate.WaitAsync(ct);
        try
        {
            await groups.RemoveFromGroupAsync(connection, SignalRGroups.Conversation(conversation), ct);
            room.Connections.Remove(connection);
        }
        finally { room.Gate.Release(); }
    }

    public async Task DisconnectedAsync(string connection)
    {
        foreach (var room in _rooms.Values)
        {
            await room.Gate.WaitAsync();
            try { room.Connections.Remove(connection); }
            finally { room.Gate.Release(); }
        }
    }
}
