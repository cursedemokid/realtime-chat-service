using ChatService.Application.Dto;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application;

internal static class ParticipantMuteEnricher
{
    public static async Task EnrichWithViewerMutesAsync(
       IReadOnlyList<ParticipantDto> dtos,
       ConversationId conversationId,
       ChatUserId viewerId,
       IParticipantNotificationMuteRepository muteRepository,
       CancellationToken ct)
    {
        foreach (var dto in dtos)
        {
            var mutedUserId = new ChatUserId(viewerId.TenantId, dto.Id);
            dto.IsNotificationsMutedByViewer = await muteRepository.IsMutedAsync(
                conversationId, viewerId, mutedUserId, ct);
        }
    }
}
