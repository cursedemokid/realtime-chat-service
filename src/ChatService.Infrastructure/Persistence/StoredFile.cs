namespace ChatService.Infrastructure.Persistence;

public sealed class StoredFile
{
    public Guid Id { get; set; }
    public string Owner { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long Size { get; set; }
}
