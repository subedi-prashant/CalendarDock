using System.Text.RegularExpressions;

namespace CalendarDock.Core;

public static partial class JoinLinkExtractor
{
    [GeneratedRegex(@"https://teams\.microsoft\.com/l/meetup-join/[^\s<>""']+|https://teams\.live\.com/meet/[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex TeamsRegex();

    [GeneratedRegex(@"https://meet\.google\.com/[a-z0-9\-]+(\?[^\s<>""']*)?", RegexOptions.IgnoreCase)]
    private static partial Regex MeetRegex();

    [GeneratedRegex(@"https://[a-z0-9.\-]*zoom\.us/j/[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex ZoomRegex();

    public static (string? Url, JoinProvider Provider) Extract(params string?[] texts)
    {
        foreach (var text in texts)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var match = TeamsRegex().Match(text);
            if (match.Success)
            {
                return (match.Value, JoinProvider.Teams);
            }

            match = MeetRegex().Match(text);
            if (match.Success)
            {
                return (match.Value, JoinProvider.GoogleMeet);
            }

            match = ZoomRegex().Match(text);
            if (match.Success)
            {
                return (match.Value, JoinProvider.Zoom);
            }
        }

        return (null, JoinProvider.None);
    }
}
