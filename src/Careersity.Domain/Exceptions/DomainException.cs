namespace Careersity.Domain.Exceptions;

/// <summary>Represents a violation of a domain relationship or state invariant.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
