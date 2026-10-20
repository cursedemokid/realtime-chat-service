namespace ChatService.Domain.Enums;

public enum ConversationAuditAction
{
    ParticipantJoined = 0,
    ParticipantLeft = 1,
    ParticipantAdded = 2,
    ParticipantRemoved = 3,
    RoleChanged = 4,
    StatusChanged = 5,
    TitleChanged = 6,
    DescriptionChanged = 7,
    AvatarChanged = 8,
    MessageDeleted = 9
}
