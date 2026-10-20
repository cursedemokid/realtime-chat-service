using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Enums;
using MediatR;

namespace ChatService.Application.Handlers;

public class UpdatePresenceHandler : IRequestHandler<UpdatePresenceCommand, PresenceDto>
{
    private readonly IPresenceService _presenceService;
    private readonly IConversationRealtimeNotifier _notifier;

    public UpdatePresenceHandler(IPresenceService presenceService, IConversationRealtimeNotifier notifier)
    {
        _presenceService = presenceService;
        _notifier = notifier;
    }

    public async Task<PresenceDto> Handle(UpdatePresenceCommand cmd, CancellationToken ct)
    {
        PresenceDto presence;
        if (cmd.ClearManualStatus)
            presence = await _presenceService.ClearManualStatusAsync(cmd.ParticipantId, cmd.ParticipantType, ct);
        else if (cmd.Status.HasValue)
            presence = await _presenceService.SetManualStatusAsync(cmd.ParticipantId, cmd.ParticipantType, cmd.Status.Value, ct);
        else
            presence = await _presenceService.GetOrCreateAsync(cmd.ParticipantId, cmd.ParticipantType, ct);

        await _notifier.NotifyPresenceChangedAsync(presence, ct);
        return presence;
    }
}
