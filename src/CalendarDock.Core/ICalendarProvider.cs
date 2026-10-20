namespace CalendarDock.Core;

public interface ICalendarProvider
{
    CalendarSource Source { get; }

    bool IsSignedIn { get; }

    Task<bool> SignInAsync(CancellationToken cancellationToken = default);

    Task SignOutAsync();

    Task<IReadOnlyList<Meeting>> GetMeetingsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
