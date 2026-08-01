namespace Careersity.Domain.Common;

internal static class Guard
{
    internal static Guid NotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty) throw new ArgumentException("Value cannot be an empty GUID.", parameterName);
        return value;
    }

    internal static string Required(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", parameterName);
        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength) throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", parameterName);
        return trimmed;
    }

    internal static string? Optional(string? value, int maximumLength, string parameterName)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength) throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", parameterName);
        return trimmed.Length == 0 ? null : trimmed;
    }

    internal static int Positive(int value, string parameterName)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than zero.");
        return value;
    }

    internal static int NonNegative(int value, string parameterName)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
        return value;
    }

    internal static string Slug(string? value, int maximumLength, string parameterName) =>
        Required(value, maximumLength, parameterName).ToLowerInvariant();
}
