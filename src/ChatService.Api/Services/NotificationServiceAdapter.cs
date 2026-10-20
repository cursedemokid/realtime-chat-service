using System.Text.Json;
using ChatService.Application.Services;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Repositories;

namespace ChatService.Api.Services;

public class NotificationServiceAdapter : INotificationService
{
    private readonly INotificationPublisher _publisher;
    private readonly IParticipantNotificationMuteRepository _muteRepository;

    public NotificationServiceAdapter(
        INotificationPublisher publisher,
        IParticipantNotificationMuteRepository muteRepository)
    {
        _publisher = publisher;
        _muteRepository = muteRepository;
    }

    public async Task NotifyNewMessage(Conversation conversation, Message message)
    {
        var senderId = message.Author.UserId;
        var senderType = message.Author.Type;

        foreach (var participant in conversation.Participants.Where(p => p.IsActive))
        {
            if (participant.UserId == senderId && participant.Type == senderType)
                continue;

            if (participant.IsMuted)
                continue;

            if (await _muteRepository.IsMutedAsync(
                    conversation.Id,
                    participant.UserId,
                    senderId
                    ))
                continue;

            var payload = JsonSerializer.Serialize(new
            {
                conversationId = conversation.Id.Value,
                messageId = message.Id.Value,
                senderId,
                senderType = senderType.ToString()
            });

            await _publisher.PublishNotificationRequestedAsync(new NotificationRequestedMessage
            {
                UserId = participant.UserId.ToString(),
                NotificationType = "chat.new_message",
                Payload = payload
            });
        }
    }

    public Task NotifyConversationStatusChanged(Conversation conversation)
    {
        return Task.CompletedTask;
    }
}
