using CalendarDock.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace CalendarDock.App.Services;

public static class NotificationService
{
    public static void Register()
    {
        AppNotificationManager.Default.NotificationInvoked += (s, args) =>
        {
            if (args.Arguments.TryGetValue("join", out var url) && Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            {
                _ = Windows.System.Launcher.LaunchUriAsync(uri);
            }
        };
        AppNotificationManager.Default.Register();
    }

    public static void ShowReminder(Meeting meeting)
    {
        var minutes = Math.Max(0, (int)Math.Ceiling((meeting.Start - DateTimeOffset.Now).TotalMinutes));
        var builder = new AppNotificationBuilder()
            .AddText(meeting.Title)
            .AddText(minutes <= 0 ? "Starting now" : $"Starts in {minutes} min");
        if (meeting.JoinUrl != null)
        {
            builder.AddButton(new AppNotificationButton("Join").AddArgument("join", meeting.JoinUrl));
        }

        AppNotificationManager.Default.Show(builder.BuildNotification());
    }
}

