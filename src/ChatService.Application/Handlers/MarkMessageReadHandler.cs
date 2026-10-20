using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class MarkMessagesReadHandler : IRequestHandler<MarkMessagesReadCommand>
{
    private readonly IMessageRepository _messageRepo;
    private readonly IUnitOfWork _unitOfWork;

    public MarkMessagesReadHandler(IMessageRepository messageRepo, IUnitOfWork unitOfWork)
    {
        _messageRepo = messageRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkMessagesReadCommand cmd, CancellationToken ct)
    {
        var messages = await _messageRepo.GetMessagesPendingReadReceiptAsync(
            ConversationId.From(cmd.ConversationId),
            cmd.ReaderId,
            ct);
        var unread = messages.Where(m =>
            !(m.Author.UserId == cmd.ReaderId && m.Author.Type == cmd.ReaderType) && !m.IsDeleted);
        foreach (var msg in unread)
        {
            msg.AddReadReceipt(cmd.ReaderId, cmd.ReaderType);
            await _messageRepo.UpdateAsync(msg, ct);
        }

        await _unitOfWork.SaveChangesWithDomainEventsAsync(ct);
    }
}
