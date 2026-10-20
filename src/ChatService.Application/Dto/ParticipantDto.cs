using ChatService.Domain.Enums;
namespace ChatService.Application.Dto;

public class ParticipantDto
{
    public long Id { get; set; }
    public ParticipantType Type { get; set; }
    public ConversationRole Role { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool IsMuted { get; set; }
    public bool IsWriteBlocked { get; set; }
    /// <summary>Текущий зритель замутил уведомления от этого участника в чате.</summary>
    public bool IsNotificationsMutedByViewer { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? LeftAt { get; set; }
    public PresenceStatus? PresenceStatus { get; set; }
    public DateTime? LastSeenAt { get; set; }
}
