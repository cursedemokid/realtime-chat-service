using ChatService.Domain.Aggregates;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.ValueObjects;

public sealed class ReplyReference
{
    public MessageId ReferencedMessageId { get; private set; } = default!;
    public string AuthorDisplayName { get; private set; } = null!;
    public string? TextSnippet { get; private set; }
    public bool IsDeleted { get; private set; }

    private ReplyReference()
    {
    }

    public ReplyReference(MessageId referencedMessageId, string authorDisplayName, string? textSnippet, bool isDeleted)
    {
        ReferencedMessageId = referencedMessageId;
        AuthorDisplayName = authorDisplayName ?? throw new ArgumentNullException(nameof(authorDisplayName));
        TextSnippet = textSnippet;
        IsDeleted = isDeleted;
    }

    public static ReplyReference FromMessage(Message message)
    {
        var snippet = message.Content.Text;
        if (!string.IsNullOrEmpty(snippet) && snippet.Length > 120)
            snippet = snippet[..120];

        return new ReplyReference(
            message.Id,
            message.Author.DisplayName,
            snippet,
            message.IsDeleted);
    }
}
