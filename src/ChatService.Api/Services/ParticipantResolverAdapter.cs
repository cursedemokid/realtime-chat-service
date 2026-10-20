using ChatService.Api.Services.Clients;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using ChatService.Application.Services;

namespace ChatService.Api.Services;

public class ParticipantResolverAdapter : IParticipantResolver
{
    private readonly IUserService _userService;
    private readonly IBloggerService _bloggerService;

    public ParticipantResolverAdapter(IUserService userService, IBloggerService bloggerService)
    {
        _userService = userService;
        _bloggerService = bloggerService;
    }

    public async Task<ResolvedParticipantInfo> ResolveAsync(ChatUserId userId, ParticipantType type, CancellationToken ct = default)
    {
        if (type == ParticipantType.Executor)
        {
            var blogger = await _bloggerService.GetBloggerByIdAsync(userId.UserId);
            if (blogger == null)
                throw new InvalidOperationException($"Blogger '{userId}' not found.");
            return new ResolvedParticipantInfo(userId, type, blogger.Username, blogger.AvatarUrl);
        }

        var user = await _userService.GetUserByIdAsync(userId.UserId);
        if (user == null)
            throw new InvalidOperationException($"User '{userId}' not found.");
        return new ResolvedParticipantInfo(userId, type, user.FullName, user.AvatarUrl);
    }

    public async Task<bool> ExistsAsync(ChatUserId userId, ParticipantType type, CancellationToken ct = default)
    {
        if (type == ParticipantType.Executor)
            return await _bloggerService.GetBloggerByIdAsync(userId.UserId) != null;
        return await _userService.GetUserByIdAsync(userId.UserId) != null;
    }
}