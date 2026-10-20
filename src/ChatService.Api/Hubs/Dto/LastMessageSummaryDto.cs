using System;

namespace ChatService.Api.Hubs.Dto;

public record LastMessageSummaryDto(string MessageId, string? TextPreview, DateTime CreatedAt, ConversationParticipantDto? Author);