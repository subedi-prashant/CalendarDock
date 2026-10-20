using CalendarDock.Core;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace CalendarDock.Providers;

public sealed class GoogleCalendarProvider : ICalendarProvider
{
    private readonly ProviderSettings _settings;
    private UserCredential? _credential;

    public GoogleCalendarProvider(ProviderSettings settings)
    {
        _settings = settings;
    }

    public CalendarSource Source => CalendarSource.Google;

    public bool IsSignedIn => _credential != null;

    private FileDataStore Store => new(Path.Combine(_settings.TokenCacheDirectory, "google"), true);

    public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.GoogleClientId))
        {
            return false;
        }

        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets { ClientId = _settings.GoogleClientId, ClientSecret = _settings.GoogleClientSecret },
            new[] { CalendarService.Scope.CalendarEventsReadonly },
            "user",
            cancellationToken,
            Store);
        return true;
    }

    public async Task SignOutAsync()
    {
        await Store.ClearAsync();
        _credential = null;
    }

    public async Task<IReadOnlyList<Meeting>> GetMeetingsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (_credential == null)
        {
            return Array.Empty<Meeting>();
        }

        using var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = _credential,
            ApplicationName = "CalendarDock"
        });

        var request = service.Events.List("primary");
        request.TimeMinDateTimeOffset = from;
        request.TimeMaxDateTimeOffset = to;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.MaxResults = 100;
        var events = await request.ExecuteAsync(cancellationToken);

        var meetings = new List<Meeting>();
        foreach (var item in events.Items ?? new List<Google.Apis.Calendar.v3.Data.Event>())
        {
            if (item.Status == "cancelled")
            {
                continue;
            }

            var isAllDay = item.Start.DateTimeDateTimeOffset == null;
            var start = item.Start.DateTimeDateTimeOffset ?? DateTimeOffset.Parse(item.Start.Date);
            var end = item.End.DateTimeDateTimeOffset ?? DateTimeOffset.Parse(item.End.Date);
            var (url, provider) = JoinLinkExtractor.Extract(item.HangoutLink, item.Location, item.Description);
            if (url == null)
            {
                var entry = item.ConferenceData?.EntryPoints?.FirstOrDefault(e => e.EntryPointType == "video");
                (url, provider) = JoinLinkExtractor.Extract(entry?.Uri);
            }

            meetings.Add(new Meeting(item.Id, Source, item.Summary ?? "(No title)", start, end, isAllDay, item.Location, url, provider));
        }

        return meetings;
    }
}
