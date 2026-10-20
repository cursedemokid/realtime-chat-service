using AutoMapper;
using ChatService.Application.Abstractions;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public class PresenceService : IPresenceService
{
    private readonly IUserPresenceRepository _presenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PresenceService(
        IUserPresenceRepository presenceRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _presenceRepository = presenceRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PresenceDto> GetOrCreateAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default)
    {
        var presence = await _presenceRepository.GetAsync(participantId, ct);
        if (presence == null)
        {
            presence = new UserPresence(participantId);
            await _presenceRepository.UpsertAsync(presence, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        return Map(presence, participantType);
    }

    public async Task<PresenceDto> OnConnectedAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default)
    {
        var presence = await GetPresenceEntityAsync(participantId, ct);
        presence.OnConnected();
        await _presenceRepository.UpsertAsync(presence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(presence, participantType);
    }

    public async Task<PresenceDto> OnDisconnectedAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default)
    {
        var presence = await GetPresenceEntityAsync(participantId, ct);
        presence.OnDisconnected();
        await _presenceRepository.UpsertAsync(presence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(presence, participantType);
    }

    public async Task TouchActivityAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default)
    {
        var presence = await _presenceRepository.GetAsync(participantId, ct);
        if (presence == null)
            return;
        presence.TouchActivity();
        await _presenceRepository.UpsertAsync(presence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<PresenceDto> SetManualStatusAsync(
        ChatUserId participantId,
        ParticipantType participantType,
        PresenceStatus status,
        CancellationToken ct = default)
    {
        var presence = await GetPresenceEntityAsync(participantId, ct);
        presence.SetManualStatus(status);
        await _presenceRepository.UpsertAsync(presence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(presence, participantType);
    }

    public async Task<PresenceDto> ClearManualStatusAsync(ChatUserId participantId, ParticipantType participantType, CancellationToken ct = default)
    {
        var presence = await GetPresenceEntityAsync(participantId, ct);
        presence.ClearManualStatus();
        await _presenceRepository.UpsertAsync(presence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(presence, participantType);
    }

    public async Task SweepIdleAsync(TimeSpan idleThreshold, CancellationToken ct = default)
    {
        var online = await _presenceRepository.GetAllOnlineAsync(ct);
        var hasUpdates = false;
        foreach (var presence in online)
        {
            var previousStatus = presence.Status;
            presence.MarkAway(idleThreshold);
            if (presence.Status == previousStatus)
                continue;
            await _presenceRepository.UpsertAsync(presence, ct);
            hasUpdates = true;
        }
        if (hasUpdates)
            await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PresenceDto>> GetManyAsync(
        IEnumerable<(ChatUserId UserId, ParticipantType Type)> participants,
        CancellationToken ct = default)
    {
        var participantList = participants.Distinct().ToList();
        var presences = await _presenceRepository.GetManyAsync(
            participantList.Select(p => p.UserId).Distinct(), ct);
        var presenceByUserId = presences.ToDictionary(p => p.UserId);

        return participantList
            .Where(p => presenceByUserId.ContainsKey(p.UserId))
            .Select(p => Map(presenceByUserId[p.UserId], p.Type))
            .ToList();
    }

    private async Task<UserPresence> GetPresenceEntityAsync(ChatUserId participantId, CancellationToken ct)
    {
        var presence = await _presenceRepository.GetAsync(participantId, ct);
        if (presence != null)
            return presence;
        presence = new UserPresence(participantId);
        return presence;
    }

    private PresenceDto Map(UserPresence presence, ParticipantType participantType)
    {
        var dto = _mapper.Map<PresenceDto>(presence);
        dto.TenantId = presence.UserId.TenantId;
        dto.ParticipantType = participantType;
        return dto;
    }
}
