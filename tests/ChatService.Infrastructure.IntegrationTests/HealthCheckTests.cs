using ChatService.Api.Infrastructure;
using ChatService.Api.Infrastructure.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public sealed class HealthCheckTests
{
    [Fact]
    public async Task HealthChecks_WithoutDatabaseAndRabbit_ReturnUnhealthyInsteadOfThrowing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        var availability = HealthChecksDependencyInjection.GetAvailability(configuration);
        var databaseHealthCheck = new DatabaseHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            availability);
        var rabbitMqHealthCheck = new RabbitMqHealthCheck(configuration, availability);

        var databaseResult = await databaseHealthCheck.CheckHealthAsync(new HealthCheckContext());
        var rabbitMqResult = await rabbitMqHealthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, databaseResult.Status);
        Assert.Equal(HealthStatus.Unhealthy, rabbitMqResult.Status);
        Assert.False(availability.DatabaseConfigured);
        Assert.False(availability.RabbitMqConfigured);
    }
}
