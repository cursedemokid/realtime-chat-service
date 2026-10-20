
namespace ChatService.Domain.ValueObjects;

public sealed record ConversationId(Guid Value)
{
    public static ConversationId New() => new(Guid.CreateVersion7());
    public static ConversationId From(Guid value) => new(value);
}

public sealed record MessageId(Guid Value)
{
    public static MessageId New() => new(Guid.CreateVersion7());
    public static MessageId From(Guid value) => new(value);
}

public sealed record SupportTicketId(Guid Value)
{
    public static SupportTicketId New() => new(Guid.CreateVersion7());
    public static SupportTicketId From(Guid value) => new(value);
}