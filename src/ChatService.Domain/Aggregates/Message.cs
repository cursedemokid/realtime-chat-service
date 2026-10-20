using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.Events;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Aggregates;

public class Message : AggregateRoot<MessageId>
{
    public ConversationId ConversationId { get; private set; } = default!;
    public MessageAuthor Author { get; private set; } = null!;
    public MessageType Type { get; private set; }
    public MessageContent Content { get; private set; } = null!;
    public ReplyReference? ReplyTo { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt != null;

    private readonly List<MessageReadReceipt> _readReceipts = new();
    public IReadOnlyList<MessageReadReceipt> ReadReceipts => _readReceipts.AsReadOnly();

    private Message()
    {
    }

    public static Message Create(
        ConversationId conversationId,
        MessageAuthor author,
        MessageType type,
        MessageContent content,
        Message? parentMessage = null)
    {
        if (author == null)
            throw new DomainException("Author is required");
        if (content == null)
            throw new DomainException("Content is required");
        EnsureContentMatchesMessageType(type, content, author);

        ReplyReference? replyTo = null;
        if (parentMessage != null)
        {
            if (parentMessage.ConversationId != conversationId)
                throw new DomainException("Parent message belongs to another conversation");
            replyTo = ReplyReference.FromMessage(parentMessage);
        }

        var message = new Message
        {
            Id = MessageId.New(),
            ConversationId = conversationId,
            Author = author,
            Type = type,
            Content = content,
            ReplyTo = replyTo,
            CreatedAt = DateTime.UtcNow
        };

        message.RaiseDomainEvent(new MessageSentEvent(
            message.Id,
            message.ConversationId,
            message.Author,
            message.Content,
            message.Type));

        return message;
    }

    public void Edit(string newText, IReadOnlyList<Attachment>? newAttachments = null)
    {
        if (IsDeleted)
            throw new DomainException("Cannot edit deleted message");
        var newContent = new MessageContent(newText, newAttachments);
        EnsureContentMatchesMessageType(Type, newContent, Author);
        Content = newContent;
        EditedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MessageEditedEvent(Id, ConversationId));
    }

    public void Delete(ChatUserId actorId, ParticipantType actorType)
    {
        if (IsDeleted)
            return;
        DeletedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MessageDeletedEvent(Id, ConversationId, actorId, actorType));
    }

    public void AddReadReceipt(ChatUserId readerId, ParticipantType readerType)
    {
        if (_readReceipts.Any(r => r.ReaderId == readerId && r.ReaderType == readerType))
            return;
        _readReceipts.Add(new MessageReadReceipt(readerId, readerType));
        RaiseDomainEvent(new MessageReadEvent(Id, ConversationId, readerId, readerType));
    }

    private static void EnsureContentMatchesMessageType(
        MessageType type,
        MessageContent content,
        MessageAuthor author)
    {
        switch (type)
        {
            case MessageType.Text:
                if (string.IsNullOrWhiteSpace(content.Text))
                    throw new DomainException("Text message must include non-empty text");
                if (author.Type == ParticipantType.System)
                    throw new DomainException("System messages must use MessageType.System");
                break;
            case MessageType.System:
                if (author.Type != ParticipantType.System)
                    throw new DomainException("System message type requires system author");
                if (string.IsNullOrWhiteSpace(content.Text))
                    throw new DomainException("System message must include text");
                break;
            case MessageType.Attachment:
                if (!content.Attachments.Any())
                    throw new DomainException("Attachment message must include at least one attachment");
                break;
            case MessageType.DateProposal:
                if (string.IsNullOrWhiteSpace(content.Text))
                    throw new DomainException("Date proposal message must include text");
                if (author.Type == ParticipantType.System)
                    throw new DomainException("System cannot author date proposal messages");
                break;
            default:
                throw new DomainException($"Unsupported message type {type}");
        }
    }
}
