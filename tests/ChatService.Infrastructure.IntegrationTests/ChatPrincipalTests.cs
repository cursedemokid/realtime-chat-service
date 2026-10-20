using System.Security.Claims;
using ChatService.Api.Authentication;
using ChatService.Domain.Enums;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public class ChatPrincipalTests
{
    [Theory]
    [InlineData("Customer", ParticipantType.Customer)]
    [InlineData("Performer", ParticipantType.Executor)]
    public void UserService_claims_are_resolved(string performanceType, ParticipantType expectedType)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "321"),
            new Claim("org_id", "a927b445-e332-4da8-a4e7-c97fb7107ed3"),
            new Claim("performance_type", performanceType)
        ]));

        var resolved = ChatPrincipal.TryResolve(principal, out var userId, out var participantType);

        Assert.True(resolved);
        Assert.Equal(321, userId.UserId);
        Assert.Equal("a927b445-e332-4da8-a4e7-c97fb7107ed3", userId.TenantId);
        Assert.Equal(expectedType, participantType);
    }

    [Fact]
    public void UserService_uuid_subject_is_resolved_to_stable_chat_id()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "019fcbc3-35c1-7008-a8c6-bb6e9e419d58"),
            new Claim("org_id", "019fcbc3-3558-7428-9b84-757360ab602f"),
            new Claim("performance_type", "Customer")
        ]));

        Assert.True(ChatPrincipal.TryResolve(principal, out var first, out var participantType));
        Assert.True(ChatPrincipal.TryResolve(principal, out var second, out _));
        Assert.True(first.UserId > 0);
        Assert.Equal(first, second);
        Assert.Equal(ParticipantType.Customer, participantType);
    }
}
