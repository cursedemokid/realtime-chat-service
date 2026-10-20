namespace ChatService.Api.Infrastructure;

public sealed class InfrastructureAvailability
{
    public bool DatabaseConfigured { get; init; }

    public bool RabbitMqConfigured { get; init; }

    public string? DatabaseReason { get; init; }

    public string? RabbitMqReason { get; init; }
}
