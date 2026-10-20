using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Dto;

public class SupportTicketDto
{
    public Guid Id { get; set; }
    public Guid? ConversationId { get; set; }
    public TicketParticipantDto Initiator { get; set; } = null!;
    public string Theme { get; set; } = null!;
    public TicketStatus Status { get; set; }
    public string? AssignedManagerId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int EscalationLevel { get; set; }
    public string? CloseReason { get; set; }
}

public class TicketParticipantDto
{
    public string ChatUserId { get; set; } = null!;
    public ParticipantType Type { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}