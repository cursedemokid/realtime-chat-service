using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record MarkSingleMessageReadCommand(
    Guid ConversationId,
    Guid MessageId,
    ChatUserId ReaderId,
    ParticipantType ReaderType) : IRequest;
