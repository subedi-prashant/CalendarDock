using CalendarDock.Core;

namespace CalendarDock.Tests;

public class CoreTests
{
    private static Meeting Make(string id, string title, int startMinutes, string? url = null, CalendarSource source = CalendarSource.Google)
    {
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero).AddMinutes(startMinutes);
        return new Meeting(id, source, title, start, start.AddMinutes(30), false, null, url, url == null ? JoinProvider.None : JoinProvider.Teams);
    }

    [Theory]
    [InlineData("Join: https://teams.microsoft.com/l/meetup-join/19%3ameeting_abc/0?context=x ok", JoinProvider.Teams)]
    [InlineData("https://meet.google.com/abc-defg-hij", JoinProvider.GoogleMeet)]
    [InlineData("see https://us02web.zoom.us/j/123456?pwd=x", JoinProvider.Zoom)]
    [InlineData("nothing here", JoinProvider.None)]
    public void Extract_FindsProvider(string text, JoinProvider expected)
    {
        Assert.Equal(expected, JoinLinkExtractor.Extract(null, text).Provider);
    }

    [Fact]
    public void Merge_DedupesAndSorts()
    {
        var merged = MeetingAggregator.Merge(new[]
        {
            Make("2", "Standup", 60),
            Make("1", "Sync", 0),
            Make("3", "standup", 60, "https://x", CalendarSource.Outlook)
        });

        Assert.Equal(2, merged.Count);
        Assert.Equal("Sync", merged[0].Title);
        Assert.NotNull(merged[1].JoinUrl);
    }

    [Fact]
    public void Reminder_FiresOnce()
    {
        var scheduler = new ReminderScheduler();
        var meeting = Make("1", "A", 10);
        var now = meeting.Start.AddMinutes(-4);

        Assert.Single(scheduler.GetDue(new[] { meeting }, now));
        Assert.Empty(scheduler.GetDue(new[] { meeting }, now));
        Assert.Empty(scheduler.GetDue(new[] { Make("2", "B", 60) }, now));
    }
}
