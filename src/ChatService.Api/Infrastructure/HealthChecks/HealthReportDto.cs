namespace ChatService.Api.Infrastructure.HealthChecks;

public record HealthReportDto(string Status, double TotalDurationMs, Dictionary<string, ReportEntryDto> Entries);
