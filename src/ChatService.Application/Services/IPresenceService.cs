using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface IPresenceService
{
    Task<PresenceDto> GetOrCreateAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default);
    Task<PresenceDto> OnConnectedAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default);
    Task<PresenceDto> OnDisconnectedAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default);
    Task TouchActivityAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default);
    Task<PresenceDto> SetManualStatusAsync(ChatUserId participantId, ParticipantType participantType, PresenceStatus status, CancellationToken ct = default);
    Task<PresenceDto> ClearManualStatusAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default);
    Task SweepIdleAsync(TimeSpan idleThreshold, CancellationToken ct = default);

    Task<IReadOnlyList<PresenceDto>> GetManyAsync(
        IEnumerable<(ChatUserId UserId, ParticipantType Type)> participants,
        CancellationToken ct = default);
}
