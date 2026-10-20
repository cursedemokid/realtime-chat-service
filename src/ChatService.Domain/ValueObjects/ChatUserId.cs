using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ChatService.Domain.ValueObjects;

public sealed record ChatUserId
{
    public string TenantId { get; }
    public long UserId { get; }

    public ChatUserId(string tenantId, long userId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId is required", nameof(tenantId));
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));
        TenantId = tenantId;
        UserId = userId;
    }

    public override string ToString() => $"{TenantId}:{UserId}";

    public static ChatUserId FromString(string value)
    {
        var parts = value.Split(':');
        if (parts.Length == 1 && Guid.TryParse(value, out var legacyId))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(legacyId.ToString("D")));
            var numericId = BinaryPrimitives.ReadInt64BigEndian(hash) & long.MaxValue;
            return new ChatUserId("legacy", numericId);
        }
        return new ChatUserId(parts[0], long.Parse(parts[1]));
    }
}
