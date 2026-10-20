using Microsoft.Extensions.Configuration;

namespace ChatService.Infrastructure.Persistence;

internal static class ChatDbConnection
{
    public static string Resolve(IConfiguration configuration) =>
        configuration.GetConnectionString("ChatDb")
        ?? configuration.GetConnectionString("ChatDbMigration")
        ?? throw new InvalidOperationException(
            "Connection string 'ChatDb' or 'ChatDbMigration' is not configured. " +
            "Add it to appsettings.Development.json or appsettings.Development.local.json.");
}
