using CalendarDock.Core;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace CalendarDock.App;

public sealed partial class MeetingsWindow : Window
{
    private const int WIDTH = 420;
    private const int HEIGHT = 560;

    public MeetingsWindow()
    {
        InitializeComponent();
        Title = "Calendar Dock";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, false);
        }

        ExtendsContentIntoTitleBar = true;
        AppWindow.IsShownInSwitchers = false;
        Activated += (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                AppWindow.Hide();
            }
        };
        Closed += (_, e) =>
        {
            e.Handled = true;
            AppWindow.Hide();
        };
        App.Host.Changed += Render;
        Render();
    }

    public void Toggle()
    {
        if (AppWindow.IsVisible)
        {
            AppWindow.Hide();
            return;
        }

        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        var dpi = (double)Win32.GetDpiForWindow(WindowNative.GetWindowHandle(this)) / 96;
        var width = (int)(WIDTH * dpi);
        var height = (int)(HEIGHT * dpi);
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(area.X + area.Width - width - 12, area.Y + area.Height - height - 12, width, height));
        Render();
        AppWindow.Show();
        Activate();
        _ = App.Host.RefreshAsync();
    }

    private async void OnRefresh(object sender, RoutedEventArgs e)
    {
        await App.Host.RefreshAsync();
    }

    private void Render()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            MeetingList.Children.Clear();
            var now = DateTimeOffset.Now;
            var upcoming = App.Host.Meetings.Where(m => m.End > now).ToList();
            if (upcoming.Count == 0)
            {
                MeetingList.Children.Add(new TextBlock { Text = App.Host.Providers.Any(p => p.IsSignedIn) ? "No upcoming meetings." : "Sign in to a calendar below.", Opacity = 0.7 });
            }

            foreach (var group in upcoming.GroupBy(m => m.Start.LocalDateTime.Date))
            {
                MeetingList.Children.Add(new TextBlock { Text = group.Key == DateTime.Today ? "Today" : group.Key.ToString("dddd, MMM d"), Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"], Margin = new Thickness(0, 8, 0, 0) });
                foreach (var meeting in group)
                {
                    MeetingList.Children.Add(BuildCard(meeting));
                }
            }

            AccountPanel.Children.Clear();
            foreach (var provider in App.Host.Providers)
            {
                var button = new Button { Content = provider.IsSignedIn ? $"Sign out {provider.Source}" : $"Sign in {provider.Source}" };
                button.Click += async (_, _) => await App.Host.ToggleSignInAsync(provider);
                AccountPanel.Children.Add(button);
            }
        });
    }

    private static UIElement BuildCard(Meeting meeting)
    {
        var time = meeting.IsAllDay ? "All day" : $"{meeting.Start.LocalDateTime:h:mm tt} - {meeting.End.LocalDateTime:h:mm tt}";
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = meeting.Title, TextTrimming = TextTrimming.CharacterEllipsis });
        text.Children.Add(new TextBlock { Text = $"{time}  ·  {meeting.Source}", Opacity = 0.7, FontSize = 12 });

        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(text);
        if (meeting.JoinUrl != null && Uri.TryCreate(meeting.JoinUrl, UriKind.Absolute, out var uri))
        {
            var join = new Button { Content = $"Join {meeting.JoinProvider}", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
            join.Click += async (_, _) => await Windows.System.Launcher.LaunchUriAsync(uri);
            Grid.SetColumn(join, 1);
            grid.Children.Add(join);
        }

        return new Border
        {
            Child = grid,
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(6),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"]
        };
    }
}

internal static partial class Win32
{
    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    internal static partial uint GetDpiForWindow(IntPtr hwnd);
}
