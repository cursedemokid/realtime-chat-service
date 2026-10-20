namespace ChatService.Application.Dto;

public class ReplyToDto
{
    public Guid MessageId { get; set; }
    public string AuthorDisplayName { get; set; } = null!;
    public string? TextSnippet { get; set; }
    public bool IsDeleted { get; set; }
}
