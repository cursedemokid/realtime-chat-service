using ChatService.Domain.Enums;

namespace ChatService.Domain;

public static class ParticipantModerationRules
{
    public static bool CanModerateByType(ParticipantType actor, ParticipantType target)
    {
        var actorLevel = GetLevel(actor);
        var targetLevel = GetLevel(target);
        if (actorLevel < 0 || targetLevel < 0)
            return false;

        return actorLevel <= targetLevel;
    }

    public static bool IsProtectedManagerInSupport(ConversationKind kind, ParticipantType target, bool isSelf)
        => kind == ConversationKind.Support
           && target == ParticipantType.Manager
           && !isSelf;

    private static int GetLevel(ParticipantType type)
        => type switch
        {
            ParticipantType.Manager => 0,
            ParticipantType.Customer => 1,
            ParticipantType.Executor => 2,
            _ => -1
        };
}
