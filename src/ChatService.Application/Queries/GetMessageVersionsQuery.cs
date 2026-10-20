using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Queries;

public record GetMessageVersionsQuery(
    Guid ConversationId,
    Guid MessageId,
    ChatUserId ViewerId,
    ParticipantType ViewerType,
    int Skip = 0,
    int Take = 50) : IRequest<MessageVersionsPageDto>;

public class MessageVersionsPageDto
{
    public IReadOnlyList<MessageVersionDto> Items { get; set; } = Array.Empty<MessageVersionDto>();
    public int TotalCount { get; set; }
}
