using ChatService.Domain.Enums;

namespace ChatService.Api.Hubs.Dto;

public record ConversationParticipantDto(string? Id, ParticipantType Type, string? DisplayName, string? AvatarUrl);