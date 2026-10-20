namespace ChatService.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Access denied") : base(message) { }
}
