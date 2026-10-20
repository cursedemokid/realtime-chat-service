namespace ChatService.Application.Dto;

public class ConversationParticipantsPageDto
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public IReadOnlyList<ParticipantDto> Items { get; set; } = Array.Empty<ParticipantDto>();
}
