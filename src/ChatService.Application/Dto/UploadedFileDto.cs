namespace ChatService.Application.Dto;

public class UploadedFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long Size { get; set; }
    public string? DownloadUrl { get; set; }
}
