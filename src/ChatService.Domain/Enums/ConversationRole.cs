namespace ChatService.Domain.Enums;

/// <summary>
/// Роль участника внутри конкретного чата (не путать с глобальным ParticipantType).
/// </summary>
public enum ConversationRole
{
    Owner = 0,
    Admin = 1,
    Moderator = 2,
    Member = 3,
    ReadOnly = 4
}
