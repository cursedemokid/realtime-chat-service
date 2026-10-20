using ChatService.Domain.Aggregates;

namespace ChatService.Application.Services;

public interface INotificationService
{
    Task NotifyNewMessage(Conversation conversation, Message message);
    Task NotifyConversationStatusChanged(Conversation conversation);
}