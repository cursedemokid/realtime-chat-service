using AutoMapper;
using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using ChatService.Application.Abstractions;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using DomainAttachment = ChatService.Domain.ValueObjects.Attachment;

namespace ChatService.Application.Handlers;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, MessageDto>
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWorkExecutor _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAttachmentAccess _attachmentAccess;

    public SendMessageHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        INotificationService notificationService,
        IUnitOfWorkExecutor unitOfWork,
        IMapper mapper, IAttachmentAccess attachmentAccess)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _attachmentAccess = attachmentAccess;
    }

    public async Task<MessageDto> Handle(SendMessageCommand cmd, CancellationToken ct)
    {
        var conversationId = ConversationId.From(cmd.ConversationId);
        var conversation = await _conversationRepo.GetByIdAsync(conversationId, ct);
        if (conversation == null) throw new NotFoundException(nameof(Conversation), cmd.ConversationId);
        if (!conversation.IsOpen) throw new DomainException("Conversation is closed");

        ConversationAuthorization.EnsureHasPermission(
            conversation, cmd.SenderId, cmd.SenderType, ConversationPermission.SendMessage);

        if (!conversation.IsParticipant(cmd.SenderId, cmd.SenderType))
            throw new DomainException("Sender is not a participant");

        var participant = conversation.GetActiveParticipant(cmd.SenderId, cmd.SenderType)!;
        if (participant.IsWriteBlocked)
            throw new DomainException("You are not allowed to send messages in this conversation");

        var author = MessageAuthor.FromParticipant(participant);

        await _attachmentAccess.ValidateAsync(cmd.Attachments, cmd.SenderId, cmd.SenderType, ct);
        var attachments = cmd.Attachments?.Select(a => new DomainAttachment(
            Guid.Parse(a.Id), a.Name ?? "", a.Size ?? 0, a.MimeType ?? "",
            (AttachmentType)a.AttachmentType!, a.Url)).ToList();

        var content = new MessageContent(cmd.Text, attachments);
        var type = cmd.IsDateProposal
            ? MessageType.DateProposal
            : attachments is { Count: > 0 }
                ? MessageType.Attachment
                : MessageType.Text;

        var message = Message.Create(conversationId, author, type, content);

        var dto = await _unitOfWork.RunAsync(async innerCt =>
        {
            await _messageRepo.AddAsync(message, innerCt);
            conversation.UpdateLastActivity();
            await _conversationRepo.UpdateAsync(conversation, innerCt);
            var mapped = _mapper.Map<MessageDto>(message);
            var otherCount = MessageReadStatusMapper.CountOtherActiveParticipants(
                conversation, cmd.SenderId);
            MessageReadStatusMapper.EnrichMessageDto(mapped, message, cmd.SenderId, cmd.SenderType, otherCount);
            return mapped;
        }, ct);

        await _notificationService.NotifyNewMessage(conversation, message);

        return dto;
    }
}
