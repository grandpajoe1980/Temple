using System.Text.RegularExpressions;

namespace Temple.Domain.Shared;

public static partial class Slug
{
    [GeneratedRegex("[^a-z0-9]+")] private static partial Regex NonAlphanum();

    public static string From(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var lower = input.Trim().ToLowerInvariant();
        var replaced = NonAlphanum().Replace(lower, "-");
        var trimmed = replaced.Trim('-');
        return trimmed[..Math.Min(80, trimmed.Length)];
    }
}
