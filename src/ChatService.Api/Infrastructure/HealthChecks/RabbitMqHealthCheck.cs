using ChatService.Api.Infrastructure;
using ChatService.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace ChatService.Api.Infrastructure.HealthChecks;

internal sealed class RabbitMqHealthCheck(
    IConfiguration configuration,
    InfrastructureAvailability availability) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!availability.RabbitMqConfigured)
        {
            return HealthCheckResult.Unhealthy(
                availability.RabbitMqReason ?? "RabbitMQ is not configured.");
        }

        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()!;
        var virtualHost = string.IsNullOrWhiteSpace(options.VirtualHost) ? "/" : options.VirtualHost;
        var data = new Dictionary<string, object>
        {
            ["host"] = options.HostName,
            ["port"] = options.Port,
            ["virtualHost"] = virtualHost
        };

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = virtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(2),
                HandshakeContinuationTimeout = TimeSpan.FromSeconds(2),
                ContinuationTimeout = TimeSpan.FromSeconds(2),
                AutomaticRecoveryEnabled = false,
                TopologyRecoveryEnabled = false,
                ClientProvidedName = "chat-service-health-check"
            };

            await using var connection = await factory.CreateConnectionAsync(timeout.Token);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: timeout.Token);

            return HealthCheckResult.Healthy("RabbitMQ AMQP connection is available.", data);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ AMQP connection check failed.",
                exception,
                data);
        }
    }
}
