using ChatService.Api.Infrastructure;
using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace ChatService.Api.Infrastructure.HealthChecks;

internal sealed class DatabaseHealthCheck(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    InfrastructureAvailability availability) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!availability.DatabaseConfigured)
        {
            return HealthCheckResult.Unhealthy(
                availability.DatabaseReason ?? "PostgreSQL database connection string is not configured.");
        }

        var connectionString = configuration.GetConnectionString("ChatDb")
            ?? configuration.GetConnectionString("ChatDbMigration");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy(
                availability.DatabaseReason ?? "PostgreSQL database connection string is not configured.");
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ConversationDbContext>();
        if (dbContext is null)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL database services are not registered.");
        }

        var data = new Dictionary<string, object>
        {
            ["provider"] = dbContext.Database.ProviderName ?? "unknown",
            ["database"] = dbContext.Database.GetDbConnection().Database
        };

        try
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            if (connectionStringBuilder.Timeout is <= 0 or > 2)
                connectionStringBuilder.Timeout = 2;

            if (connectionStringBuilder.CommandTimeout is <= 0 or > 2)
                connectionStringBuilder.CommandTimeout = 2;

            connectionStringBuilder.Pooling = false;

            await using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            data["canConnect"] = true;

            return HealthCheckResult.Healthy("PostgreSQL database connection is available.", data);
        }
        catch (Exception exception)
        {
            data["canConnect"] = false;

            return HealthCheckResult.Unhealthy(
                "PostgreSQL database connection check failed.",
                exception,
                data);
        }
    }
}
