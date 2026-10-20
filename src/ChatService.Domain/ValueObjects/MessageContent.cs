using ChatService.Domain.Exceptions;

namespace ChatService.Domain.ValueObjects;

public sealed class MessageContent
{
    public string? Text { get; }
    public IReadOnlyList<Attachment> Attachments { get; }

    private MessageContent()
    {
        Attachments = new List<Attachment>();
    }

    public MessageContent(string? text, IReadOnlyList<Attachment>? attachments = null)
    {
        Text = text;
        Attachments = attachments?.ToList() ?? new List<Attachment>();
        if (string.IsNullOrWhiteSpace(text) && !Attachments.Any())
            throw new DomainException("Message must have text or at least one attachment");
    }
}
