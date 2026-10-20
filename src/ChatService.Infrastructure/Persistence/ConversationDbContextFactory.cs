using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ChatService.Infrastructure.Persistence;

public class ConversationDbContextFactory : IDesignTimeDbContextFactory<ConversationDbContext>
{
    public ConversationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var apiPath = ResolveApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = ChatDbConnection.Resolve(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<ConversationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new ConversationDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiProjectPath()
    {
        var current = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(current, "ChatService.Api"),
            Path.GetFullPath(Path.Combine(current, "..", "ChatService.Api")),
            Path.GetFullPath(Path.Combine(current, "..", "..", "ChatService.Api"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(Path.Combine(path, "appsettings.json")))
                return path;
        }

        return current;
    }
}
