using System;

namespace ChatService.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}