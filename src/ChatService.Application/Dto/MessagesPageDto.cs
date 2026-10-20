namespace ChatService.Application.Dto;

public class MessagesPageDto
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public IReadOnlyList<MessageDto> Items { get; set; } = Array.Empty<MessageDto>();
}
