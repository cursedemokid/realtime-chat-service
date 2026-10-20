namespace ChatService.Application.Dto;

public class ConversationsPageDto
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public IReadOnlyList<ConversationSummaryDto> Items { get; set; } = Array.Empty<ConversationSummaryDto>();
    public int TotalUnreadCount { get; set; }
}
