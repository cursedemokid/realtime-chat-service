using MediatR;
using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Commands;

public record SendMessageCommand(
    Guid ConversationId,
    ChatUserId SenderId,
    ParticipantType SenderType,
    string? Text,
    List<MessageAttachmentDto>? Attachments,
    bool IsDateProposal) : IRequest<MessageDto>;