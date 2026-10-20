namespace CalendarDock.Core;

public enum CalendarSource
{
    Google,
    Outlook
}

public enum JoinProvider
{
    None,
    Teams,
    GoogleMeet,
    Zoom
}

public sealed record Meeting(
    string Id,
    CalendarSource Source,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay,
    string? Location,
    string? JoinUrl,
    JoinProvider JoinProvider);
