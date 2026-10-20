namespace ChatService.Application.Dto;

public class ConversationInboxUpdateDto
{
    public Guid ConversationId { get; set; }
    public int UnreadCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public MessageDto? LastMessage { get; set; }
}
