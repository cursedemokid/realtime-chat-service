namespace ChatService.Api.Infrastructure.HealthChecks;

public record ReportEntryDto(
    string Status,
    string? Description,
    double DurationMs,
    IEnumerable<string> Tags,
    IReadOnlyDictionary<string, object> Data,
    EntryExceptionDto? Error);
