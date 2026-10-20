using System.Text;
using System.Text.Json.Nodes;
using ChatService.Api.Filters;
using ChatService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ChatService.Api.Controllers;

[ApiController, DevelopmentOnly, Route("api/demo/rabbitmq")]
public sealed class RabbitDemoController(IOptions<RabbitMqOptions> options) : ControllerBase
{
    public const string QueueName = "demo.chat.notifications";

    [HttpPost("prepare")]
    public async Task<IActionResult> Prepare(CancellationToken ct)
    {
        var settings = options.Value;
        await using var connection = await Factory().CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(settings.EventsExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(QueueName, settings.EventsExchange, settings.NotificationRequestedRoutingKey, cancellationToken: ct);
        return Ok(new { exchange = settings.EventsExchange, queue = QueueName, routingKey = settings.NotificationRequestedRoutingKey,
            nextStep = "Отправьте сообщение от Alex. Событие уведомления для Sam появится в этой очереди." });
    }

    [HttpPost("consume")]
    public async Task<IActionResult> ConsumeOne(CancellationToken ct)
    {
        await using var connection = await Factory().CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        // This is an explicit demo consumer: the returned event is acknowledged and removed from the demo queue.
        var result = await channel.BasicGetAsync(QueueName, autoAck: true, cancellationToken: ct);
        return Ok(new { queue = QueueName, hasMessage = result is not null,
            body = result is null ? null : JsonNode.Parse(Encoding.UTF8.GetString(result.Body.Span)) });
    }

    private ConnectionFactory Factory() => new()
    {
        HostName = options.Value.HostName, Port = options.Value.Port, UserName = options.Value.UserName,
        Password = options.Value.Password, VirtualHost = options.Value.VirtualHost
    };
}
