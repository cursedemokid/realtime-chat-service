using System.Text;
using System.Text.Json;
using ChatService.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ChatService.Infrastructure.Messaging;

public class RabbitMqNotificationPublisher : INotificationPublisher
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqNotificationPublisher> _logger;

    public RabbitMqNotificationPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqNotificationPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishNotificationRequestedAsync(NotificationRequestedMessage message, CancellationToken ct = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            await using var connection = await factory.CreateConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync(
                _options.EventsExchange,
                ExchangeType.Topic,
                durable: true,
                cancellationToken: ct);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.EventId.ToString()
            };

            await channel.BasicPublishAsync(
                _options.EventsExchange,
                _options.NotificationRequestedRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);

            _logger.LogDebug(
                "Published notification.requested for user {UserId}, type {NotificationType}",
                message.UserId,
                message.NotificationType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish notification.requested to RabbitMQ");
        }
    }
}
