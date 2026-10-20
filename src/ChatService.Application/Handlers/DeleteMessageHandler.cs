using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using static ChatService.Application.ConversationAuthorization;

namespace ChatService.Application.Handlers;

public class DeleteMessageHandler : IRequestHandler<DeleteMessageCommand>
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMessageHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IUnitOfWork unitOfWork)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteMessageCommand cmd, CancellationToken ct)
    {
        var conversationId = ConversationId.From(cmd.ConversationId);
        var conversation = await _conversationRepo.GetByIdAsync(conversationId, ct);
        if (conversation == null) throw new NotFoundException(nameof(Conversation), cmd.ConversationId);

        var message = await _messageRepo.GetByIdAsync(MessageId.From(cmd.MessageId), ct);
        if (message == null || message.ConversationId != conversationId)
            throw new NotFoundException(nameof(Message), cmd.MessageId);

        EnsureCanRead(conversation, cmd.ActorId, cmd.ActorType);
        MessageAuthorization.EnsureCanDelete(conversation, message, cmd.ActorId, cmd.ActorType);

        message.Delete(cmd.ActorId, cmd.ActorType);
        await _messageRepo.UpdateAsync(message, ct);
        await _unitOfWork.SaveChangesWithDomainEventsAsync(ct);
    }
}
