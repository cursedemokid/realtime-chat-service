namespace ChatService.Api.Services.Clients;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(long id);
}
public record UserDto(long Id, string FullName, string? AvatarUrl, string? KeycloakId);

public class UserServiceClient : IUserService
{
    public Task<UserDto?> GetUserByIdAsync(long id) =>
        Task.FromResult<UserDto?>(new UserDto(id, $"User {id}", null, null));
}