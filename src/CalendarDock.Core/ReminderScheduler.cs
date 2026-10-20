namespace CalendarDock.Core;

public sealed class ReminderScheduler
{
    private readonly HashSet<string> _notified = new();

    public TimeSpan LeadTime { get; set; } = TimeSpan.FromMinutes(5);

    public IReadOnlyList<Meeting> GetDue(IEnumerable<Meeting> meetings, DateTimeOffset now)
    {
        var due = new List<Meeting>();
        foreach (var meeting in meetings)
        {
            if (meeting.IsAllDay || meeting.Start <= now.AddMinutes(-1) || meeting.Start - now > LeadTime)
            {
                continue;
            }

            if (_notified.Add($"{meeting.Source}:{meeting.Id}:{meeting.Start:O}"))
            {
                due.Add(meeting);
            }
        }

        return due;
    }
}
