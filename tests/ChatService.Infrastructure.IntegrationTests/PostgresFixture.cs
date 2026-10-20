using ChatService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    public ConversationDbContext DbContext { get; private set; } = null!;
    public string? ConnectionString { get; private set; }
    public bool IsConfigured { get; private set; }

    public async Task InitializeAsync()
    {
        ConnectionString = ResolveConnectionString();
        IsConfigured = !string.IsNullOrWhiteSpace(ConnectionString);

        if (!IsConfigured)
            return;

        var options = new DbContextOptionsBuilder<ConversationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        DbContext = new ConversationDbContext(options);
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
            await DbContext.DisposeAsync();
    }

    private static string? ResolveConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("CHAT_DB_TEST");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var apiPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ChatService.Api"));

        if (!File.Exists(Path.Combine(apiPath, "appsettings.json")))
            return null;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        try
        {
            return configuration.GetConnectionString("ChatDb")
                ?? configuration.GetConnectionString("ChatDbMigration");
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
