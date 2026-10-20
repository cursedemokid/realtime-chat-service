using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChatService.Api.Infrastructure.HealthChecks;

internal static class HealthCheckResponseWriter
{
    public static async Task Write(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthReportDto(
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new ReportEntryDto(
                    entry.Value.Status.ToString(),
                    entry.Value.Description,
                    entry.Value.Duration.TotalMilliseconds,
                    entry.Value.Tags,
                    entry.Value.Data,
                    entry.Value.Exception is null
                        ? null
                        : new EntryExceptionDto(
                            entry.Value.Exception.GetType().Name,
                            entry.Value.Exception.Message))));

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true },
            context.RequestAborted);
    }
}
