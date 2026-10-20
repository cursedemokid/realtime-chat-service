namespace ChatService.Application.Dto;

public class SearchConversationsResultDto
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<ConversationSummaryDto> Items { get; set; } = new();
}
