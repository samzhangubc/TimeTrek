using System.Globalization;

namespace ThymeMe.Domain.Common;

public static class DomainText
{
    public const int MaximumNameLength = 200;
    public const int MaximumDescriptionLength = 16_384;

    public static string RequiredName(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        string normalized = value.Trim();

        if (normalized.Length is 0 or > MaximumNameLength || ContainsUnsafeControl(normalized))
        {
            throw new DomainValidationException(
                $"{parameterName} must contain 1-{MaximumNameLength.ToString(CultureInfo.InvariantCulture)} printable characters.");
        }

        return normalized;
    }

    public static string? OptionalDescription(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > MaximumDescriptionLength || ContainsUnsafeControl(value, allowLineBreaks: true))
        {
            throw new DomainValidationException(
                $"Descriptions may contain at most {MaximumDescriptionLength.ToString(CultureInfo.InvariantCulture)} characters.");
        }

        return value.Length == 0 ? null : value;
    }

    private static bool ContainsUnsafeControl(string value, bool allowLineBreaks = false)
    {
        foreach (char character in value)
        {
            if (char.IsControl(character) &&
                !(allowLineBreaks && character is '\r' or '\n' or '\t'))
            {
                return true;
            }
        }

        return false;
    }
}
