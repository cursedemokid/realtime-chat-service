using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public record MarkMessagesReadCommand(Guid ConversationId, ChatUserId ReaderId, ParticipantType ReaderType) : IRequest;