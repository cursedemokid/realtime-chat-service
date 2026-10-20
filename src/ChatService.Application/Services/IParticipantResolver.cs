using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface IParticipantResolver
{
    Task<ResolvedParticipantInfo> ResolveAsync(ChatUserId userId, ParticipantType type, CancellationToken ct);
    Task<bool> ExistsAsync(ChatUserId userId, ParticipantType type, CancellationToken ct);
}

public class ResolvedParticipantInfo
{
    public ChatUserId UserId { get; }
    public ParticipantType Type { get; }
    public string DisplayName { get; }
    public string? AvatarUrl { get; }

    public ResolvedParticipantInfo(ChatUserId userId, ParticipantType type, string displayName, string? avatarUrl)
    {
        UserId = userId;
        Type = type;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }
}