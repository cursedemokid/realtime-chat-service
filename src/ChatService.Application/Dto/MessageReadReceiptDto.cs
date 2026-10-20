using ChatService.Domain.Enums;

namespace ChatService.Application.Dto;

public class MessageReadReceiptDto
{
    public long ReaderId { get; set; }
    public ParticipantType ReaderType { get; set; }
    public DateTime ReadAt { get; set; }
}
