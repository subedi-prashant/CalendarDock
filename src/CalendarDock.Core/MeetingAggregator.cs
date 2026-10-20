namespace CalendarDock.Core;

public sealed class MeetingAggregator
{
    private readonly IEnumerable<ICalendarProvider> _providers;

    public MeetingAggregator(IEnumerable<ICalendarProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<Meeting>> GetMeetingsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Where(p => p.IsSignedIn).Select(async p =>
        {
            try
            {
                return await p.GetMeetingsAsync(from, to, cancellationToken);
            }
            catch (Exception)
            {
                return Array.Empty<Meeting>();
            }
        });

        var results = await Task.WhenAll(tasks);
        return Merge(results.SelectMany(r => r));
    }

    public static IReadOnlyList<Meeting> Merge(IEnumerable<Meeting> meetings)
    {
        // The same meeting can appear in both calendars when invites are shared; keep the one with a join link.
        return meetings
            .GroupBy(m => (m.Title.Trim().ToLowerInvariant(), m.Start, m.End))
            .Select(g => g.OrderByDescending(m => m.JoinUrl != null).First())
            .OrderBy(m => m.Start)
            .ThenBy(m => m.Title)
            .ToList();
    }

    public static IReadOnlyList<Meeting> Upcoming(IEnumerable<Meeting> meetings, DateTimeOffset now, int count)
    {
        return meetings.Where(m => m.End > now && !m.IsAllDay).OrderBy(m => m.Start).Take(count).ToList();
    }
}
