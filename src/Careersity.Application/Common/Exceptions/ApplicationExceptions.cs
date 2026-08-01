namespace Careersity.Application.Common.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class RequestValidationException(string message) : Exception(message);
public sealed class ServiceUnavailableException(string message) : Exception(message);
