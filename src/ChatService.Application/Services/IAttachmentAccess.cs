using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface IAttachmentAccess
{
    Task ValidateAsync(IEnumerable<MessageAttachmentDto>? attachments, ChatUserId userId,
        ParticipantType type, CancellationToken cancellationToken);
}
