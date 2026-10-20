using ChatService.Domain.Enums;

namespace ChatService.Domain;

public static class ConversationRolePermissions
{
    private const ConversationPermission DirectParticipantPermissions =
        ConversationPermission.Read
        | ConversationPermission.SendMessage
        | ConversationPermission.ManageOwnSettings
        | ConversationPermission.EditOwnMessage
        | ConversationPermission.DeleteOwnMessage
        | ConversationPermission.EditMetadata;

    private const ConversationPermission SupportManagerPermissions =
        ConversationPermission.Read
        | ConversationPermission.SendMessage
        | ConversationPermission.ManageParticipants
        | ConversationPermission.ChangeStatus
        | ConversationPermission.Archive
        | ConversationPermission.ManageOwnSettings;

    private static readonly IReadOnlyDictionary<ConversationRole, ConversationPermission> RolePermissions =
        new Dictionary<ConversationRole, ConversationPermission>
        {
            [ConversationRole.Owner] = ConversationPermission.Read
                | ConversationPermission.SendMessage
                | ConversationPermission.ManageParticipants
                | ConversationPermission.MuteParticipant
                | ConversationPermission.ChangeRoles
                | ConversationPermission.ChangeStatus
                | ConversationPermission.Archive
                | ConversationPermission.ManageOwnSettings
                | ConversationPermission.EditOwnMessage
                | ConversationPermission.DeleteOwnMessage
                | ConversationPermission.DeleteAnyMessage
                | ConversationPermission.EditMetadata,

            [ConversationRole.Admin] = ConversationPermission.Read
                | ConversationPermission.SendMessage
                | ConversationPermission.ManageParticipants
                | ConversationPermission.MuteParticipant
                | ConversationPermission.ChangeStatus
                | ConversationPermission.Archive
                | ConversationPermission.ManageOwnSettings
                | ConversationPermission.EditOwnMessage
                | ConversationPermission.DeleteOwnMessage
                | ConversationPermission.DeleteAnyMessage
                | ConversationPermission.EditMetadata,

            [ConversationRole.Moderator] = ConversationPermission.Read
                | ConversationPermission.SendMessage
                | ConversationPermission.RemoveParticipant
                | ConversationPermission.MuteParticipant
                | ConversationPermission.ManageOwnSettings
                | ConversationPermission.EditOwnMessage
                | ConversationPermission.DeleteOwnMessage
                | ConversationPermission.DeleteAnyMessage,

            [ConversationRole.Member] = ConversationPermission.Read
                | ConversationPermission.SendMessage
                | ConversationPermission.ManageOwnSettings
                | ConversationPermission.EditOwnMessage
                | ConversationPermission.DeleteOwnMessage,

            [ConversationRole.ReadOnly] = ConversationPermission.Read
                | ConversationPermission.ManageOwnSettings
        };

    public static bool Has(ConversationRole role, ConversationPermission permission)
        => RolePermissions.TryGetValue(role, out var granted) && granted.HasFlag(permission);

    public static bool HasDirectParticipantPermission(ConversationPermission permission)
        => DirectParticipantPermissions.HasFlag(permission);

    public static bool HasSupportManagerPermission(ConversationPermission permission)
        => SupportManagerPermissions.HasFlag(permission);

    public static bool CanManageTarget(ConversationRole actorRole, ConversationRole targetRole)
        => actorRole switch
        {
            ConversationRole.Owner => true,
            ConversationRole.Admin => targetRole is ConversationRole.Moderator or ConversationRole.Member or ConversationRole.ReadOnly,
            ConversationRole.Moderator => targetRole is ConversationRole.Member or ConversationRole.ReadOnly,
            _ => false
        };
}
