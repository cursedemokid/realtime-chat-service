using ChatService.Domain.Enums;
namespace ChatService.Application.Dto;

public class TypingUserDto
{
    public long ParticipantId { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public string DisplayName { get; set; } = null!;
}
