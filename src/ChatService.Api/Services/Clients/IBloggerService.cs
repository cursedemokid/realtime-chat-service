namespace ChatService.Api.Services.Clients;

public interface IBloggerService
{
    Task<BloggerDto?> GetBloggerByIdAsync(long id);
}
public record BloggerDto(long Id, string Username, string? AvatarUrl);

public class BloggerServiceClient : IBloggerService
{
    public Task<BloggerDto?> GetBloggerByIdAsync(long id) =>
        Task.FromResult<BloggerDto?>(new BloggerDto(id, $"Blogger {id}", null));
}