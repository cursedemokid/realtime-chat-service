using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record EditMessageCommand(
    Guid ConversationId,
    Guid MessageId,
    ChatUserId ActorId,
    ParticipantType ActorType,
    string Text,
    List<MessageAttachmentDto>? Attachments = null) : IRequest<MessageDto>;
