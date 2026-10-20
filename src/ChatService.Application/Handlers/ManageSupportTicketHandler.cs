using AutoMapper;
using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class ManageSupportTicketHandler : IRequestHandler<ManageSupportTicketCommand, SupportTicketDto>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IParticipantResolver _participantResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ManageSupportTicketHandler(
        ISupportTicketRepository ticketRepository,
        IParticipantResolver participantResolver,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _ticketRepository = ticketRepository;
        _participantResolver = participantResolver;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SupportTicketDto> Handle(ManageSupportTicketCommand request, CancellationToken cancellationToken)
    {
        if (request.ActorType != ParticipantType.Manager)
            throw new ForbiddenException("Only a manager can change support ticket lifecycle");

        var ticket = await _ticketRepository.GetByIdAsync(SupportTicketId.From(request.TicketId), cancellationToken);
        if (ticket == null) throw new NotFoundException(nameof(ticket), request.TicketId);

        var actorUserId = new ChatUserId(request.ActorId.TenantId, request.ActorId.UserId);

        switch (request.Action)
        {
            case SupportTicketAction.AssignManager:
                var managerInfo = await _participantResolver.ResolveAsync(actorUserId, ParticipantType.Manager, cancellationToken);
                var managerParticipant = new TicketParticipant(
                    managerInfo.UserId,
                    managerInfo.Type,
                    managerInfo.DisplayName,
                    managerInfo.AvatarUrl);
                ticket.AssignManager(managerParticipant);
                break;
            case SupportTicketAction.StartProgress:
                ticket.InProgress();
                break;
            case SupportTicketAction.Escalate:
                ticket.Escalate();
                break;
            case SupportTicketAction.Close:
                ticket.Close(request.Reason ?? "Closed");
                break;
            case SupportTicketAction.Reject:
                ticket.Reject(request.Reason ?? "Rejected");
                break;
            default:
                throw new DomainException($"Unsupported action {request.Action}");
        }

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesWithDomainEventsAsync(cancellationToken);
        return _mapper.Map<SupportTicketDto>(ticket);
    }
}