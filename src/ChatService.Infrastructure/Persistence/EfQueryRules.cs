namespace ChatService.Infrastructure.Persistence;

/// <summary>
/// EF Core LINQ guidelines for this project:
/// - In IQueryable use domain value objects with configured converters (ConversationId, MessageId, etc.).
/// - Do not call .Value or .From() inside Where/Select/Contains on IQueryable.
/// - Project to Guid or primitives only after ToListAsync / in-memory.
/// </summary>
internal static class EfQueryRules
{
}
