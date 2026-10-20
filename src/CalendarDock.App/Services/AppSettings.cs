using System.Text.Json;
using CalendarDock.Core;
using CalendarDock.Providers;

namespace CalendarDock.App.Services;

public sealed class AppSettings
{
    public GoogleSettings Google { get; set; } = new();

    public MicrosoftSettings Microsoft { get; set; } = new();

    public int ReminderMinutes { get; set; } = 5;

    public int RefreshMinutes { get; set; } = 5;

    public static AppSettings Load()
    {
        var settings = Read("appsettings.json");
        var local = Read("appsettings.local.json");
        settings.Google.ClientId = Pick(local.Google.ClientId, settings.Google.ClientId);
        settings.Google.ClientSecret = Pick(local.Google.ClientSecret, settings.Google.ClientSecret);
        settings.Microsoft.ClientId = Pick(local.Microsoft.ClientId, settings.Microsoft.ClientId);
        return settings;
    }

    private static string Pick(string local, string fallback)
    {
        return string.IsNullOrWhiteSpace(local) ? fallback : local;
    }

    private static AppSettings Read(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppSettings()
            : new AppSettings();
    }
}

public sealed class GoogleSettings
{
    public string ClientId { get; set; } = "";

    public string ClientSecret { get; set; } = "";
}

public sealed class MicrosoftSettings
{
    public string ClientId { get; set; } = "";
}
