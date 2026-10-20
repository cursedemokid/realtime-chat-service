using ChatService.Api.Infrastructure.HealthChecks;
using ChatService.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChatService.Api.Infrastructure;

public static class HealthChecksDependencyInjection
{
    public static IServiceCollection AddChatHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(GetAvailability(configuration));

        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("ChatService is running."),
                tags: ["live"])
            .AddCheck<DatabaseHealthCheck>(
                "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "postgres", "dependency"])
            .AddCheck<RabbitMqHealthCheck>(
                "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "rabbitmq", "dependency"]);

        return services;
    }

    internal static InfrastructureAvailability GetAvailability(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChatDb")
            ?? configuration.GetConnectionString("ChatDbMigration");
        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();
        var missingRabbitMqFields = GetMissingRabbitMqFields(rabbitMqOptions).ToArray();

        return new InfrastructureAvailability
        {
            DatabaseConfigured = !string.IsNullOrWhiteSpace(connectionString),
            DatabaseReason = string.IsNullOrWhiteSpace(connectionString)
                ? "Connection string 'ChatDb' or 'ChatDbMigration' is not configured."
                : null,
            RabbitMqConfigured = missingRabbitMqFields.Length == 0,
            RabbitMqReason = missingRabbitMqFields.Length == 0
                ? null
                : $"RabbitMQ settings are incomplete: {string.Join(", ", missingRabbitMqFields)}."
        };
    }

    private static IEnumerable<string> GetMissingRabbitMqFields(RabbitMqOptions? options)
    {
        if (options is null)
        {
            yield return RabbitMqOptions.SectionName;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(options.HostName))
            yield return $"{RabbitMqOptions.SectionName}:HostName";

        if (options.Port is < 1 or > 65535)
            yield return $"{RabbitMqOptions.SectionName}:Port";

        if (string.IsNullOrWhiteSpace(options.UserName))
            yield return $"{RabbitMqOptions.SectionName}:UserName";

        if (string.IsNullOrWhiteSpace(options.Password))
            yield return $"{RabbitMqOptions.SectionName}:Password";
    }
}
