using AutoMapper;
using ChatService.Application.Commands;
using ChatService.Application.Services;
using ChatService.Application.Dto;
using ChatService.Application.Abstractions;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using DomainAttachment = ChatService.Domain.ValueObjects.Attachment;

namespace ChatService.Application.Handlers;

public class EditMessageHandler : IRequestHandler<EditMessageCommand, MessageDto>
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IMessageVersionRepository _messageVersionRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAttachmentAccess _attachmentAccess;

    public EditMessageHandler(
        IConversationRepository conversationRepo,
        IMessageRepository messageRepo,
        IMessageVersionRepository messageVersionRepo,
        IUnitOfWork unitOfWork,
        IMapper mapper, IAttachmentAccess attachmentAccess)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _messageVersionRepo = messageVersionRepo;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _attachmentAccess = attachmentAccess;
    }

    public async Task<MessageDto> Handle(EditMessageCommand cmd, CancellationToken ct)
    {
        var conversationId = ConversationId.From(cmd.ConversationId);
        var conversation = await _conversationRepo.GetByIdAsync(conversationId, ct);
        if (conversation == null) throw new NotFoundException(nameof(Conversation), cmd.ConversationId);

        var message = await _messageRepo.GetByIdAsync(MessageId.From(cmd.MessageId), ct);
        if (message == null || message.ConversationId != conversationId)
            throw new NotFoundException(nameof(Message), cmd.MessageId);

        ConversationAuthorization.EnsureCanRead(conversation, cmd.ActorId, cmd.ActorType);
        MessageAuthorization.EnsureCanEdit(conversation, message, cmd.ActorId, cmd.ActorType);

        var latestVersion = await _messageVersionRepo.GetLatestVersionNumberAsync(message.Id, ct);
        if (latestVersion == 0)
        {
            var originalVersion = new MessageVersion(
                message.Id,
                1,
                message.Content.Text,
                message.Content.Attachments.ToList(),
                message.Author.UserId.UserId,
                message.Author.Type,
                message.CreatedAt);
            await _messageVersionRepo.AddAsync(originalVersion, ct);
            latestVersion = 1;
        }

        await _attachmentAccess.ValidateAsync(cmd.Attachments, cmd.ActorId, cmd.ActorType, ct);
        var attachments = cmd.Attachments?.Select(a => new DomainAttachment(
            Guid.Parse(a.Id), a.Name ?? "", a.Size ?? 0, a.MimeType ?? "",
            (AttachmentType)a.AttachmentType!, a.Url)).ToList();

        message.Edit(cmd.Text, attachments);

        var editedVersion = new MessageVersion(
            message.Id,
            latestVersion + 1,
            message.Content.Text,
            message.Content.Attachments.ToList(),
            cmd.ActorId.UserId,
            cmd.ActorType,
            message.EditedAt!.Value);
        await _messageVersionRepo.AddAsync(editedVersion, ct);
        await _messageRepo.UpdateAsync(message, ct);
        await _unitOfWork.SaveChangesWithDomainEventsAsync(ct);

        var dto = _mapper.Map<MessageDto>(message);
        MessageReadStatusMapper.EnrichMessageDto(dto, message, cmd.ActorId, cmd.ActorType);
        return dto;
    }
}
