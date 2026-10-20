using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Application.Services;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using static ChatService.Application.ConversationAuthorization;

namespace ChatService.Application.Handlers;

public class MarkSingleMessageReadHandler : IRequestHandler<MarkSingleMessageReadCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInboxUpdateService _inboxUpdateService;

    public MarkSingleMessageReadHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepo,
        IUnitOfWork unitOfWork,
        IInboxUpdateService inboxUpdateService)
    {
        _conversationRepository = conversationRepository;
        _messageRepo = messageRepo;
        _unitOfWork = unitOfWork;
        _inboxUpdateService = inboxUpdateService;
    }

    public async Task Handle(MarkSingleMessageReadCommand cmd, CancellationToken ct)
    {
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId.From(cmd.ConversationId), ct);
        if (conversation == null) throw new NotFoundException(nameof(conversation), cmd.ConversationId);

        EnsureCanRead(conversation, cmd.ReaderId, cmd.ReaderType);

        var message = await _messageRepo.GetByIdAsync(MessageId.From(cmd.MessageId), ct);
        if (message == null || message.ConversationId != conversation.Id)
            throw new NotFoundException(nameof(Message), cmd.MessageId);

        if (message.IsDeleted)
            return;

        if (message.Author.UserId == cmd.ReaderId && message.Author.Type == cmd.ReaderType)
            return;

        message.AddReadReceipt(cmd.ReaderId, cmd.ReaderType);
        await _messageRepo.UpdateAsync(message, ct);
        await _unitOfWork.SaveChangesWithDomainEventsAsync(ct);
        await _inboxUpdateService.NotifyParticipantAsync(cmd.ConversationId, cmd.ReaderId, cmd.ReaderType, ct);
    }
}
