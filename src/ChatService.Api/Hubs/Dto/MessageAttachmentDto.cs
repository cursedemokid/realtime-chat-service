using ChatService.Domain.Enums;

namespace ChatService.Api.Hubs.Dto;

public record MessageAttachmentDto(string Id, string? Name, long? Size, string? MimeType, AttachmentType? AttachmentType, string Url);