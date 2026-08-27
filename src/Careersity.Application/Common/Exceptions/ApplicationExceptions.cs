namespace Careersity.Application.Common.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class RequestValidationException(string message) : Exception(message);
public sealed class ServiceUnavailableException(string message) : Exception(message);
public sealed class ExternalServiceUnavailableException(string message) : Exception(message);
public sealed class AuthenticationFailedException(string message = "Authentication failed.") : Exception(message);
public sealed class UnauthorizedException(string message = "Authentication is required.") : Exception(message);
