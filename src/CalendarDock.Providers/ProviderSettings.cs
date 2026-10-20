namespace CalendarDock.Providers;

public sealed record ProviderSettings(
    string GoogleClientId,
    string GoogleClientSecret,
    string MicrosoftClientId,
    string TokenCacheDirectory);
