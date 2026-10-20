using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Dto;

public class MessageAuthorDto
{
    public ChatUserId Id { get; set; } = null!;
    public ParticipantType Type { get; set; }
    public ConversationRole Role { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}
