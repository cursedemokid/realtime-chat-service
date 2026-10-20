using System.Security.Claims;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;

namespace ChatService.Api.Authentication;

internal static class ChatPrincipal
{
    public static bool TryResolve(
        ClaimsPrincipal? principal,
        out ChatUserId userId,
        out ParticipantType participantType)
    {
        userId = null!;
        participantType = default;

        var idClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal?.FindFirst("sub")?.Value
            ?? principal?.FindFirst("nameid")?.Value;
        if (!TryResolveNumericUserId(idClaim, out var numericUserId))
            return false;

        var tenantId = principal?.FindFirst("tenant")?.Value
            ?? principal?.FindFirst("org_id")?.Value
            ?? "default";

        var role = principal?.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")
            ?.Value;
        var performanceType = principal?.FindFirst("performance_type")?.Value;
        var rawType = role ?? performanceType;

        ParticipantType? resolvedType = rawType?.ToLowerInvariant() switch
        {
            "manager" or "admin" => ParticipantType.Manager,
            "customer" => ParticipantType.Customer,
            "performer" or "executor" or "blogger" => ParticipantType.Executor,
            _ => null
        };
        if (!resolvedType.HasValue)
            return false;

        participantType = resolvedType.Value;
        userId = new ChatUserId(tenantId, numericUserId);
        return true;
    }

    private static bool TryResolveNumericUserId(string? value, out long userId)
    {
        if (long.TryParse(value, out userId) && userId > 0)
            return true;

        if (!Guid.TryParse(value, out var guid))
            return false;

        // Chat's persisted identity is still Int64, while UserService issues UUID subjects.
        // Hash the canonical UUID to keep the mapping stable across processes and deployments.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(guid.ToString("D")));
        userId = BinaryPrimitives.ReadInt64BigEndian(hash) & long.MaxValue;
        return userId > 0;
    }
}
