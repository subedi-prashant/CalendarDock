using CalendarDock.Core;
using CalendarDock.Providers;

namespace CalendarDock.App.Services;

public sealed class CalendarHost
{
    private readonly ReminderScheduler _reminders = new();
    private readonly MeetingAggregator _aggregator;
    private readonly AppSettings _settings;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;
    private DateTimeOffset _lastFetch = DateTimeOffset.MinValue;

    public CalendarHost(AppSettings settings, Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
    {
        _settings = settings;
        var providerSettings = new ProviderSettings(
            settings.Google.ClientId,
            settings.Google.ClientSecret,
            settings.Microsoft.ClientId,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalendarDock"));
        Providers = new ICalendarProvider[] { new GoogleCalendarProvider(providerSettings), new OutlookCalendarProvider(providerSettings) };
        _aggregator = new MeetingAggregator(Providers);
        _reminders.LeadTime = TimeSpan.FromMinutes(settings.ReminderMinutes);
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(30);
        _timer.Tick += async (_, _) => await TickAsync();
    }

    public IReadOnlyList<ICalendarProvider> Providers { get; }

    public IReadOnlyList<Meeting> Meetings { get; private set; } = Array.Empty<Meeting>();

    public event Action? Changed;

    public event Action<Meeting>? ReminderDue;

    public void Start()
    {
        _timer.Start();
        _ = TickAsync(true);
    }

    public async Task RefreshAsync()
    {
        var now = DateTimeOffset.Now;
        Meetings = await _aggregator.GetMeetingsAsync(now.Date, now.Date.AddDays(2));
        _lastFetch = DateTimeOffset.Now;
        Changed?.Invoke();
    }

    public async Task TickAsync(bool force = false)
    {
        if (force || DateTimeOffset.Now - _lastFetch > TimeSpan.FromMinutes(_settings.RefreshMinutes))
        {
            await RefreshAsync();
        }

        foreach (var meeting in _reminders.GetDue(Meetings, DateTimeOffset.Now))
        {
            ReminderDue?.Invoke(meeting);
        }
    }

    public async Task ToggleSignInAsync(ICalendarProvider provider)
    {
        if (provider.IsSignedIn)
        {
            await provider.SignOutAsync();
        }
        else
        {
            await provider.SignInAsync();
        }

        await RefreshAsync();
    }
}
