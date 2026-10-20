using ChatService.Domain.Enums;
using MediatR;
using ChatService.Application.Dto;

namespace ChatService.Application.Commands;

public class CreateConversationCommand : IRequest<ConversationSummaryDto>
{
    public string TenantId { get; set; } = string.Empty;
    public long CreatorId { get; set; }
    public ParticipantType CreatorType { get; set; }
    public ConversationKind Kind { get; set; }
    public string? ContextReference { get; set; }
    public IReadOnlyList<CreateConversationParticipantRequest>? AdditionalParticipants { get; set; }
}

public class CreateConversationParticipantRequest
{
    public long Id { get; set; }
    public ParticipantType Type { get; set; }
    public ConversationRole Role { get; set; }
}