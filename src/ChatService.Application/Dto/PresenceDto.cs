using ChatService.Domain.Enums;
namespace ChatService.Application.Dto;

public class PresenceDto
{
    public string TenantId { get; set; } = "default";
    public long ParticipantId { get; set; }
    public ParticipantType? ParticipantType { get; set; }
    public PresenceStatus Status { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}
