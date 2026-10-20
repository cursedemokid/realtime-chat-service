using ChatService.Domain.Enums;

namespace ChatService.Api.Hubs.Dto;

public record ParticipantDto(long Id, ParticipantType Type, string DisplayName, string? AvatarUrl = null);