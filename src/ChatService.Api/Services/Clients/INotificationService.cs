namespace ChatService.Api.Services.Clients;

public interface INotificationService
{
    Task NotifyNewMessageAsync(long recipientId, string message);
}
public class NotificationServiceClient : INotificationService
{
    public Task NotifyNewMessageAsync(long recipientId, string message) => Task.CompletedTask;
}