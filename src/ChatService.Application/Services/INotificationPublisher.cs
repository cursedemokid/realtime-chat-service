namespace ChatService.Application.Services;

public interface INotificationPublisher
{
    Task PublishNotificationRequestedAsync(NotificationRequestedMessage message, CancellationToken cancellationToken = default);
}
