using ChatService.Domain.Abstractions;
using ChatService.Domain.Enums;
using ChatService.Domain.Events;
using ChatService.Domain.Exceptions;
using ChatService.Domain.ValueObjects;

namespace ChatService.Domain.Aggregates;

public class Conversation : AggregateRoot<ConversationId>
{
    private readonly List<ConversationParticipant> _participants = new();

    public ConversationKind Kind { get; private set; }
    public string? ContextReference { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public ConversationStatus Status { get; private set; }
    public IReadOnlyList<ConversationParticipant> Participants => _participants.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }

    private Conversation() { }

    private Conversation(ConversationKind kind, string? contextReference, IEnumerable<ConversationParticipant> participants)
    {
        Id = ConversationId.New();
        Kind = kind;
        ContextReference = contextReference;
        Status = ConversationStatus.Open;
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = CreatedAt;
        foreach (var p in participants)
            AddParticipantCore(p, isRejoin: false);
    }

    public static Conversation CreateSupportTicket(string? contextReference, ConversationParticipant initiator)
    {
        if (initiator.Type != ParticipantType.Customer && initiator.Type != ParticipantType.Executor)
            throw new DomainException("Support ticket must be started by customer or executor");
        return new Conversation(ConversationKind.Support, contextReference, new[] { initiator });
    }

    public static Conversation CreateOrderConversation(string orderId, ConversationParticipant customer, ConversationParticipant executor)
    {
        if (customer.Type != ParticipantType.Customer)
            throw new DomainException("Customer type required");
        if (executor.Type != ParticipantType.Executor)
            throw new DomainException("Executor type required");
        EnsureChatParticipant(customer);
        EnsureChatParticipant(executor);
        if (customer.UserId == executor.UserId && customer.Type == executor.Type)
            throw new DomainException("Order conversation requires two different participants");
        return new Conversation(ConversationKind.Order, orderId, new[] { customer, executor });
    }

    public static Conversation CreateDirect(ConversationParticipant p1, ConversationParticipant p2)
    {
        EnsureChatParticipant(p1);
        EnsureChatParticipant(p2);
        if (p1.UserId == p2.UserId && p1.Type == p2.Type)
            throw new DomainException("Direct conversation requires two different participants");
        return new Conversation(ConversationKind.Direct, null, new[] { p1, p2 });
    }

    public static Conversation CreateGroup(IEnumerable<ConversationParticipant> participants)
    {
        var list = participants?.ToList() ?? throw new DomainException("Participants are required");
        if (list.Count < 2)
            throw new DomainException("Group conversation requires at least two participants");
        foreach (var p in list)
            EnsureChatParticipant(p);
        var keys = new HashSet<(ChatUserId UserId, ParticipantType Type)>();
        foreach (var p in list)
        {
            if (!keys.Add((p.UserId, p.Type)))
                throw new DomainException("Duplicate participants");
        }
        return new Conversation(ConversationKind.Group, null, list);
    }

    public static Conversation CreateEvent(string? contextReference, IEnumerable<ConversationParticipant> participants)
    {
        var list = participants?.ToList() ?? throw new DomainException("Participants are required");
        if (list.Count < 1)
            throw new DomainException("Event conversation requires at least one participant");
        foreach (var p in list)
            EnsureChatParticipant(p);
        return new Conversation(ConversationKind.Event, contextReference, list);
    }

    public ConversationParticipant? GetActiveParticipant(ChatUserId userId, ParticipantType type)
        => _participants.FirstOrDefault(p => p.UserId == userId && p.Type == type && p.IsActive);

    public bool HasPermission(ChatUserId userId, ParticipantType type, ConversationPermission permission)
    {
        if (Kind == ConversationKind.Support && type == ParticipantType.Manager)
            return ConversationRolePermissions.HasSupportManagerPermission(permission);
        if (Kind is ConversationKind.Direct or ConversationKind.Order)
            return ConversationRolePermissions.HasDirectParticipantPermission(permission);
        var participant = GetActiveParticipant(userId, type);
        return participant != null && ConversationRolePermissions.Has(participant.Role, permission);
    }

    public void AddParticipant(ConversationParticipant participant, ChatUserId actorUserId, ParticipantType actorType)
    {
        if (_participants.Any(p => p.IsActive))
            EnsureHasPermission(actorUserId, actorType, ConversationPermission.AddParticipant);
        AddParticipantCore(participant, isRejoin: false);
    }

    public void AddParticipantOnCreate(ConversationParticipant participant)
        => AddParticipantCore(participant, isRejoin: false);

    public void RemoveParticipant(
        ChatUserId targetUserId, ParticipantType targetType,
        ChatUserId actorUserId, ParticipantType actorType)
    {
        var isSelf = targetUserId == actorUserId && targetType == actorType;
        if (!isSelf)
            EnsureHasPermission(actorUserId, actorType, ConversationPermission.RemoveParticipant);

        EnsureCanModerateTarget(actorUserId, actorType, targetUserId, targetType, isSelf);

        var activeCount = _participants.Count(p => p.IsActive);
        var idx = _participants.FindIndex(p => p.UserId == targetUserId && p.Type == targetType);
        if (idx == -1)
            throw new DomainException("Participant not found");
        if (!_participants[idx].IsActive)
            throw new DomainException("Participant already left");
        if (activeCount <= 1)
            throw new DomainException("Cannot remove last active participant");

        _participants[idx].Leave();
        UpdateLastActivity();
        RaiseDomainEvent(new ConversationParticipantRemovedEvent(
            Id.Value,
            targetUserId, targetType,
            DateTime.UtcNow,
            isSelf ? null : actorUserId,
            isSelf ? null : actorType,
            isSelf));
    }

    public void ChangeStatus(ConversationStatus newStatus, ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.ChangeStatus);
        ChangeStatusCore(newStatus);
    }

    public void ChangeStatus(ConversationStatus newStatus) => ChangeStatusCore(newStatus);

    public void UpdateMetadata(
        ChatUserId actorUserId, ParticipantType actorType,
        string? title, string? description, string? avatarUrl)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.EditMetadata);
        var oldTitle = Title;
        var oldDescription = Description;
        var oldAvatarUrl = AvatarUrl;
        if (title != null) Title = title;
        if (description != null) Description = description;
        if (avatarUrl != null) AvatarUrl = avatarUrl;
        UpdateLastActivity();
        RaiseDomainEvent(new ConversationMetadataChangedEvent(
            Id, actorUserId, actorType, oldTitle, Title, oldDescription, Description, oldAvatarUrl, AvatarUrl));
    }

    public void AssignRole(
        ChatUserId targetUserId, ParticipantType targetType, ConversationRole newRole,
        ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.ChangeRoles);
        var actor = RequireActiveParticipant(actorUserId, actorType);
        var target = RequireActiveParticipant(targetUserId, targetType);
        if (!ConversationRolePermissions.CanManageTarget(actor.Role, target.Role))
            throw new DomainException("Cannot change role for this participant");
        var oldRole = target.Role;
        target.AssignRole(newRole);
        UpdateLastActivity();
        RaiseDomainEvent(new ConversationParticipantRoleChangedEvent(
            Id.Value, targetUserId, targetType, oldRole, newRole, DateTime.UtcNow));
    }

    public void MuteChat(ChatUserId actorUserId, ParticipantType actorType)
    {
        RequireActiveParticipant(actorUserId, actorType).SetMuted(true);
        UpdateLastActivity();
    }

    public void UnmuteChat(ChatUserId actorUserId, ParticipantType actorType)
    {
        RequireActiveParticipant(actorUserId, actorType).SetMuted(false);
        UpdateLastActivity();
    }

    public void BlockWrite(
        ChatUserId targetUserId, ParticipantType targetType,
        ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.MuteParticipant);
        EnsureCanModerateTarget(actorUserId, actorType, targetUserId, targetType, isSelf: false);
        RequireActiveParticipant(targetUserId, targetType).SetWriteBlocked(true);
        UpdateLastActivity();
    }

    public void UnblockWrite(
        ChatUserId targetUserId, ParticipantType targetType,
        ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.MuteParticipant);
        EnsureCanModerateTarget(actorUserId, actorType, targetUserId, targetType, isSelf: false);
        RequireActiveParticipant(targetUserId, targetType).SetWriteBlocked(false);
        UpdateLastActivity();
    }

    public void Pin(ChatUserId targetUserId, ParticipantType targetType, ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.ManageOwnSettings);
        RequireActiveParticipant(targetUserId, targetType).SetPinned(true);
        UpdateLastActivity();
    }

    public void Unpin(ChatUserId targetUserId, ParticipantType targetType, ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.ManageOwnSettings);
        RequireActiveParticipant(targetUserId, targetType).SetPinned(false);
        UpdateLastActivity();
    }

    public void Archive(ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.Archive);
        if (Status == ConversationStatus.Closed)
            throw new DomainException("Cannot archive a closed conversation");
        ChangeStatusCore(ConversationStatus.Archived);
    }

    public void Restore(ChatUserId actorUserId, ParticipantType actorType)
    {
        EnsureHasPermission(actorUserId, actorType, ConversationPermission.Archive);
        if (Status != ConversationStatus.Archived)
            throw new DomainException("Only an archived conversation can be restored");
        ChangeStatusCore(ConversationStatus.Open);
    }

    public void UpdateLastActivity() => LastActivityAt = DateTime.UtcNow;

    public bool IsParticipant(ChatUserId userId, ParticipantType type)
        => _participants.Any(p => p.UserId == userId && p.Type == type && p.IsActive);

    public bool IsOpen => Status == ConversationStatus.Open;

    private void AddParticipantCore(ConversationParticipant participant, bool isRejoin)
    {
        var idx = _participants.FindIndex(p => p.UserId == participant.UserId && p.Type == participant.Type);
        if (idx >= 0)
        {
            var existing = _participants[idx];
            if (existing.IsActive)
                throw new DomainException("Participant already in conversation");
            existing.Reactivate(participant.DisplayName, participant.AvatarUrl, participant.Role);
            UpdateLastActivity();
            RaiseDomainEvent(new ConversationParticipantAddedEvent(Id.Value, participant, true));
            return;
        }
        _participants.Add(participant);
        UpdateLastActivity();
        RaiseDomainEvent(new ConversationParticipantAddedEvent(Id.Value, participant, false));
    }

    private void ChangeStatusCore(ConversationStatus newStatus)
    {
        if (Status == newStatus) return;
        if (Status == ConversationStatus.Closed)
            throw new DomainException("Closed conversation cannot change status");
        var allowed = (Status, newStatus) switch
        {
            (ConversationStatus.Open, ConversationStatus.Closed) => true,
            (ConversationStatus.Open, ConversationStatus.Archived) => true,
            (ConversationStatus.Archived, ConversationStatus.Open) => true,
            (ConversationStatus.Archived, ConversationStatus.Closed) => true,
            _ => false
        };
        if (!allowed)
            throw new DomainException($"Invalid conversation status transition from {Status} to {newStatus}");
        var old = Status;
        Status = newStatus;
        UpdateLastActivity();
        RaiseDomainEvent(new ConversationStatusChangedEvent(Id, old, newStatus));
    }

    private void EnsureHasPermission(ChatUserId userId, ParticipantType type, ConversationPermission permission)
    {
        if (!HasPermission(userId, type, permission))
            throw new DomainException($"Missing permission: {permission}");
    }

    private void EnsureCanModerateTarget(
        ChatUserId actorUserId, ParticipantType actorType,
        ChatUserId targetUserId, ParticipantType targetType,
        bool isSelf)
    {
        if (isSelf) return;
        if (ParticipantModerationRules.IsProtectedManagerInSupport(Kind, targetType, isSelf))
            throw new DomainException("Cannot moderate manager in support conversation");
        if (!ParticipantModerationRules.CanModerateByType(actorType, targetType))
            throw new DomainException("Insufficient participant type level to moderate target");
        if (Kind is ConversationKind.Direct or ConversationKind.Order) return;
        var actor = GetActiveParticipant(actorUserId, actorType);
        var target = GetActiveParticipant(targetUserId, targetType);
        if (actor == null || target == null)
            throw new DomainException("Participant not found");
        if (!ConversationRolePermissions.CanManageTarget(actor.Role, target.Role))
            throw new DomainException("Cannot moderate participant with this role");
    }

    private ConversationParticipant RequireActiveParticipant(ChatUserId userId, ParticipantType type)
    {
        var participant = GetActiveParticipant(userId, type);
        if (participant == null)
            throw new DomainException("Participant not found");
        return participant;
    }

    private static void EnsureChatParticipant(ConversationParticipant p)
    {
        if (p.Type == ParticipantType.System)
            throw new DomainException("System cannot be a conversation participant");
    }
}