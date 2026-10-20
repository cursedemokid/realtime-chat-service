using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface ITypingIndicatorService
{
    Task<TypingUserDto?> SetTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        CancellationToken cancellationToken = default);

    Task<bool> ClearTypingAsync(
        Guid conversationId,
        ChatUserId participantId,
        ParticipantType participantType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TypingUserDto>> GetTypingUsersAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}
