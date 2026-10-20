using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Application.Services;
using ChatService.Domain.Repositories;
using MediatR;

namespace ChatService.Application.Handlers;

public class GetPresenceHandler : IRequestHandler<GetPresenceQuery, PresenceDto>
{
    private readonly IPresenceService _presenceService;

    public GetPresenceHandler(IPresenceService presenceService)
    {
        _presenceService = presenceService;
    }

    public Task<PresenceDto> Handle(GetPresenceQuery request, CancellationToken cancellationToken)
        => _presenceService.GetOrCreateAsync(request.ParticipantId, request.ParticipantType, cancellationToken);
}

public class GetUsersPresenceHandler : IRequestHandler<GetUsersPresenceQuery, IReadOnlyList<PresenceDto>>
{
    private readonly IPresenceService _presenceService;

    public GetUsersPresenceHandler(IPresenceService presenceService)
    {
        _presenceService = presenceService;
    }

    public Task<IReadOnlyList<PresenceDto>> Handle(GetUsersPresenceQuery request, CancellationToken cancellationToken)
        => _presenceService.GetManyAsync(
            request.Participants.Select(p => (p.UserId, p.Type)),
            cancellationToken);
}
