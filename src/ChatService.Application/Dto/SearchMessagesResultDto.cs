namespace ChatService.Application.Dto;

public class SearchMessagesResultDto
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public List<MessageDto> Items { get; set; } = new();
}
