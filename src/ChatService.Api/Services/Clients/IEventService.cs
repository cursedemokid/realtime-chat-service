public interface IEventService
{
    Task<EventDto?> GetEventByIdAsync(Guid eventId, CancellationToken cancellationToken = default);
}

public record EventDto(Guid Id, string Name, long? CustomerId);