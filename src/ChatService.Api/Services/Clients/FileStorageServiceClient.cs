using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ChatService.Application.Dto;
using ChatService.Application.Services;
using Microsoft.Extensions.Options;

namespace ChatService.Api.Services.Clients;

public class StorageServiceOptions
{
    public const string SectionName = "StorageService";
    public string BaseUrl { get; set; } = "http://localhost:5001";
}

public class FileStorageServiceClient : IFileStorageService
{
    private readonly HttpClient _httpClient;

    public FileStorageServiceClient(HttpClient httpClient, IOptions<StorageServiceOptions> options)
    {
        _httpClient = httpClient;
        if (_httpClient.BaseAddress == null)
            _httpClient.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<UploadedFileDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);
        form.Add(new StringContent(uploadedBy), "uploadedBy");

        var response = await _httpClient.PostAsync("api/files/upload", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var id = json.GetProperty("id").GetGuid();
        var name = json.TryGetProperty("fileName", out var fn) ? fn.GetString() ?? fileName : fileName;
        var mime = json.TryGetProperty("contentType", out var ct) ? ct.GetString() ?? contentType : contentType;
        var size = json.TryGetProperty("size", out var sz) ? sz.GetInt64() : 0L;

        var downloadUrl = await GetDownloadUrlAsync(id, cancellationToken: cancellationToken);
        return new UploadedFileDto
        {
            Id = id,
            FileName = name,
            ContentType = mime,
            Size = size,
            DownloadUrl = downloadUrl
        };
    }

    public async Task<string> GetDownloadUrlAsync(Guid fileId, int ttlSeconds = 3600, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/files/{fileId}/download-url?ttlSeconds={ttlSeconds}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return json.GetProperty("url").GetString()
            ?? throw new InvalidOperationException("Storage service returned empty download URL.");
    }
}
