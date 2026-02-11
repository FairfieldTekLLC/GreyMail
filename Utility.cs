using System.Text.RegularExpressions;

namespace GreyMail;

internal static class Utility
{
    public static Regex DomainRegex { get; } = new(".*@(?<Domain>.*)");

    public static string FindRegExp(this string text, Regex regex, string groupName, int captureIndex = 0)
    {
        MatchCollection matches = regex.Matches(text);
        return matches.Count > captureIndex ? matches[captureIndex].Groups[groupName].Captures[0].Value : "";
    }

    public static string GetDomainFromEmail(this string emailAddress)
    {
        return FindRegExp(emailAddress, DomainRegex, "Domain").ToLower();
    }
}