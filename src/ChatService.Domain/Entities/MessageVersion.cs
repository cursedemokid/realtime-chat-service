using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Entities;

public class MessageVersion
{
    public Guid Id { get; private set; }
    public MessageId MessageId { get; private set; } = default!;
    public int Version { get; private set; }
    public string? Text { get; private set; }
    public long EditorId { get; private set; }
    public ParticipantType EditorType { get; private set; }
    public DateTime SavedAt { get; private set; }
    public IReadOnlyList<Attachment> Attachments { get; private set; } = new List<Attachment>();

    private MessageVersion()
    {
    }

    public MessageVersion(
        MessageId messageId,
        int version,
        string? text,
        IReadOnlyList<Attachment>? attachments,
        long editorId,
        ParticipantType editorType,
        DateTime savedAt)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        Version = version;
        Text = text;
        EditorId = editorId;
        EditorType = editorType;
        SavedAt = savedAt;
        Attachments = attachments?.Select(a => new Attachment(a.Id, a.FileName, a.Size, a.MimeType, a.Type, a.Url)).ToList()
            ?? new List<Attachment>();
    }
}
