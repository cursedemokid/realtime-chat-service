namespace ChatService.Domain.Enums;

[Flags]
public enum ConversationPermission
{
    None = 0,
    Read = 1 << 0,
    SendMessage = 1 << 1,
    AddParticipant = 1 << 2,
    RemoveParticipant = 1 << 3,
    ChangeRoles = 1 << 4,
    ChangeStatus = 1 << 5,
    Archive = 1 << 6,
    ManageOwnSettings = 1 << 7,
    EditOwnMessage = 1 << 8,
    DeleteOwnMessage = 1 << 9,
    DeleteAnyMessage = 1 << 10,
    EditMetadata = 1 << 11,
    MuteParticipant = 1 << 12,

    ManageParticipants = AddParticipant | RemoveParticipant
}
