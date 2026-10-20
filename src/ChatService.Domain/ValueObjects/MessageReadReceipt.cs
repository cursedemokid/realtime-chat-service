using ChatService.Domain.Enums;
using ChatService.Domain.Exceptions;

namespace ChatService.Domain.ValueObjects;

public sealed class MessageReadReceipt
{
    public ChatUserId ReaderId { get; private set; } = null!;
    public ParticipantType ReaderType { get; private set; }
    public DateTime ReadAt { get; private set; }

    private MessageReadReceipt()
    {
    }

    public MessageReadReceipt(ChatUserId readerId, ParticipantType readerType, DateTime? readAt = null)
    {
        if (readerId.UserId <= 0)
            throw new DomainException("Reader id must be positive");
        ReaderId = readerId;
        ReaderType = readerType;
        ReadAt = readAt ?? DateTime.UtcNow;
    }
}
