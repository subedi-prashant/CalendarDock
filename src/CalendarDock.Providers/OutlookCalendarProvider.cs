using System.Net.Http.Headers;
using System.Text.Json;
using CalendarDock.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace CalendarDock.Providers;

public sealed class OutlookCalendarProvider : ICalendarProvider
{
    private static readonly string[] SCOPES = { "Calendars.Read" };
    private static readonly HttpClient HTTP = new();
    private readonly ProviderSettings _settings;
    private IPublicClientApplication? _app;
    private AuthenticationResult? _auth;

    public OutlookCalendarProvider(ProviderSettings settings)
    {
        _settings = settings;
    }

    public CalendarSource Source => CalendarSource.Outlook;

    public bool IsSignedIn => _auth != null;

    private async Task<IPublicClientApplication> GetAppAsync()
    {
        if (_app != null)
        {
            return _app;
        }

        _app = PublicClientApplicationBuilder.Create(_settings.MicrosoftClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "common")
            .WithDefaultRedirectUri()
            .Build();

        Directory.CreateDirectory(_settings.TokenCacheDirectory);
        var props = new StorageCreationPropertiesBuilder("outlook.cache", _settings.TokenCacheDirectory).Build();
        var helper = await MsalCacheHelper.CreateAsync(props);
        helper.RegisterCache(_app.UserTokenCache);
        return _app;
    }

    public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.MicrosoftClientId))
        {
            return false;
        }

        var app = await GetAppAsync();
        var accounts = await app.GetAccountsAsync();
        try
        {
            _auth = await app.AcquireTokenSilent(SCOPES, accounts.FirstOrDefault()).ExecuteAsync(cancellationToken);
        }
        catch (MsalUiRequiredException)
        {
            _auth = await app.AcquireTokenInteractive(SCOPES).ExecuteAsync(cancellationToken);
        }
        catch (MsalException) when (accounts.FirstOrDefault() == null)
        {
            _auth = await app.AcquireTokenInteractive(SCOPES).ExecuteAsync(cancellationToken);
        }

        return true;
    }

    public async Task SignOutAsync()
    {
        var app = await GetAppAsync();
        foreach (var account in await app.GetAccountsAsync())
        {
            await app.RemoveAsync(account);
        }

        _auth = null;
    }

    public async Task<IReadOnlyList<Meeting>> GetMeetingsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (_auth == null)
        {
            return Array.Empty<Meeting>();
        }

        var app = await GetAppAsync();
        var account = (await app.GetAccountsAsync()).FirstOrDefault();
        _auth = await app.AcquireTokenSilent(SCOPES, account).ExecuteAsync(cancellationToken);

        var url = $"https://graph.microsoft.com/v1.0/me/calendarView?startDateTime={Uri.EscapeDataString(from.UtcDateTime.ToString("o"))}&endDateTime={Uri.EscapeDataString(to.UtcDateTime.ToString("o"))}&$orderby=start/dateTime&$top=100&$select=id,subject,start,end,isAllDay,isCancelled,location,onlineMeeting,onlineMeetingUrl,bodyPreview";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        request.Headers.Add("Prefer", "outlook.timezone=\"UTC\"");
        using var response = await HTTP.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var meetings = new List<Meeting>();
        foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            if (item.GetProperty("isCancelled").GetBoolean())
            {
                continue;
            }

            var location = item.GetProperty("location").TryGetProperty("displayName", out var loc) ? loc.GetString() : null;
            var joinUrl = item.TryGetProperty("onlineMeeting", out var om) && om.ValueKind == JsonValueKind.Object && om.TryGetProperty("joinUrl", out var ju) ? ju.GetString() : null;
            var preview = item.TryGetProperty("bodyPreview", out var bp) ? bp.GetString() : null;
            var (found, provider) = JoinLinkExtractor.Extract(joinUrl, location, preview);
            meetings.Add(new Meeting(
                item.GetProperty("id").GetString()!,
                Source,
                item.GetProperty("subject").GetString() ?? "(No title)",
                ParseUtc(item.GetProperty("start")),
                ParseUtc(item.GetProperty("end")),
                item.GetProperty("isAllDay").GetBoolean(),
                location,
                found,
                provider));
        }

        return meetings;
    }

    private static DateTimeOffset ParseUtc(JsonElement value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(DateTime.Parse(value.GetProperty("dateTime").GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind), DateTimeKind.Utc));
    }
}
