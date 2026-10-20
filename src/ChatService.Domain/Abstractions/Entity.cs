namespace ChatService.Domain.Abstractions;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedDate { get; protected set; } = DateTime.UtcNow;

    protected Entity() { }
    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj) => obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    public override int GetHashCode() => Id.GetHashCode();
}