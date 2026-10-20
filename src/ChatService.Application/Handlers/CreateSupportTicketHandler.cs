using AutoMapper;
using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Application.Abstractions;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Application.Services;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class CreateSupportTicketHandler : IRequestHandler<CreateSupportTicketCommand, SupportTicketDto>
{
    private readonly ISupportTicketRepository _ticketRepo;
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IParticipantResolver _participantResolver;
    private readonly IUnitOfWorkExecutor _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSupportTicketHandler(
        ISupportTicketRepository ticketRepo,
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IParticipantResolver participantResolver,
        IUnitOfWorkExecutor unitOfWork,
        IMapper mapper)
    {
        _ticketRepo = ticketRepo;
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _participantResolver = participantResolver;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<SupportTicketDto> Handle(CreateSupportTicketCommand cmd, CancellationToken ct) =>
        _unitOfWork.RunAsync(async innerCt =>
        {
            var initiatorUserId = new ChatUserId(cmd.UserId.TenantId, cmd.UserId.UserId);
            var initiatorInfo = await _participantResolver.ResolveAsync(initiatorUserId, cmd.InitiatorType, innerCt);
            var initiatorParticipant = new ConversationParticipant(
                initiatorInfo.UserId, initiatorInfo.Type, initiatorInfo.DisplayName, initiatorInfo.AvatarUrl);
            var ticket = SupportTicket.Create(
                TicketParticipant.FromConversationParticipant(initiatorParticipant),
                cmd.Theme);

            ConversationParticipant? managerParticipant = null;
            if (cmd.AssignedManagerId.HasValue)
            {
                var managerUserId = new ChatUserId(cmd.UserId.TenantId, cmd.AssignedManagerId.Value);
                var managerInfo = await _participantResolver.ResolveAsync(managerUserId, ParticipantType.Manager, innerCt);
                managerParticipant = new ConversationParticipant(
                    managerInfo.UserId, managerInfo.Type, managerInfo.DisplayName, managerInfo.AvatarUrl);
                ticket.AssignManager(TicketParticipant.FromConversationParticipant(managerParticipant));
            }

            var conversation = Conversation.CreateSupportTicket(ticket.Id.Value.ToString(), initiatorParticipant);
            if (managerParticipant != null)
                conversation.AddParticipantOnCreate(managerParticipant);

            ticket.LinkConversation(conversation.Id);
            await _conversationRepo.AddAsync(conversation, innerCt);
            await _ticketRepo.AddAsync(ticket, innerCt);

            if (!string.IsNullOrWhiteSpace(cmd.InitialMessage))
            {
                var message = Message.Create(
                    conversation.Id,
                    MessageAuthor.FromParticipant(initiatorParticipant),
                    MessageType.Text,
                    new MessageContent(cmd.InitialMessage.Trim()));
                await _messageRepo.AddAsync(message, innerCt);
                conversation.UpdateLastActivity();
            }

            return _mapper.Map<SupportTicketDto>(ticket);
        }, ct);
}
