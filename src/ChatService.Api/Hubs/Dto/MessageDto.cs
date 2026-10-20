using System;
using System.Collections.Generic;
using ChatService.Domain.Enums;

namespace ChatService.Api.Hubs.Dto;

public class MessageDto
{
    public string Id { get; set; } = default!;
    public MessageType Type { get; set; } = MessageType.Text;
    public ConversationParticipantDto? Author { get; set; }
    public string? TextMessage { get; set; }
    public DateTime CreationDate { get; set; }
    public List<MessageAttachmentDto>? Attachments { get; set; }
    public List<string>? ReadBy { get; set; }
    public DateTime? EditedAt { get; set; }
    public MessageContextDto? Context { get; set; }
    public string? ThreadId { get; set; }
    public bool? IsAuthor { get; set; }
    public DateTime? ProposedDate { get; set; }
}