namespace ChatService.Application.Services;

public interface IFileStorageService
{
    Task<Dto.UploadedFileDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string uploadedBy,
        CancellationToken cancellationToken = default);

    Task<string> GetDownloadUrlAsync(Guid fileId, int ttlSeconds = 3600, CancellationToken cancellationToken = default);
}
