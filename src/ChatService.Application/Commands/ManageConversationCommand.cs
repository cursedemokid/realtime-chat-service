using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Commands;

public enum ConversationAction
{
    AddParticipant,
    RemoveParticipant,
    AssignRole,
    Mute,
    Unmute,
    MuteParticipant,
    UnmuteParticipant,
    BlockWrite,
    UnblockWrite,
    Pin,
    Unpin,
    Archive,
    Restore
}

public class ManageConversationCommand : IRequest
{
    public Guid ConversationId { get; set; }
    public ConversationAction Action { get; set; }
    public ChatUserId ActorUserId { get; set; } = null!;
    public ParticipantType ActorType { get; set; }
    public ChatUserId? TargetUserId { get; set; }
    public ParticipantType? TargetParticipantType { get; set; }
    public ConversationRole? NewRole { get; set; }

    public ManageConversationCommand() { }

    public ManageConversationCommand(
        Guid conversationId,
        ConversationAction action,
        ChatUserId actorUserId,
        ParticipantType actorType,
        ChatUserId? targetUserId,
        ParticipantType? targetParticipantType,
        ConversationRole? newRole)
    {
        ConversationId = conversationId;
        Action = action;
        ActorUserId = actorUserId;
        ActorType = actorType;
        TargetUserId = targetUserId;
        TargetParticipantType = targetParticipantType;
        NewRole = newRole;
    }
}
