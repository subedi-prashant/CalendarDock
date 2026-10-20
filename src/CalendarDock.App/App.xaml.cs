using CalendarDock.App.Services;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace CalendarDock.App;

public partial class App : Application
{
    private TaskbarIcon? _tray;
    private MeetingsWindow? _window;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "calendardock-crash.log"), $"{DateTime.Now}: {e.Exception}\n\n");
        };
    }

    public static CalendarHost Host { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Launch();
        }
        catch (Exception ex)
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "calendardock-crash.log"), $"{DateTime.Now}: {ex}\n\n");
            throw;
        }
    }

    private void Launch()
    {
        var settings = AppSettings.Load();
        Host = new CalendarHost(settings, DispatcherQueue.GetForCurrentThread());
        NotificationService.Register();
        Host.ReminderDue += NotificationService.ShowReminder;
        Host.Changed += WidgetProvider.UpdateAll;

        _tray = new TaskbarIcon
        {
            ToolTipText = "Calendar Dock",
            IconSource = new GeneratedIconSource { Text = "\uE787", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 36, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White), Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.RoyalBlue) },
            NoLeftClickDelay = true,
            LeftClickCommand = new RelayCommand(TogglePopup),
            ContextFlyout = BuildMenu()
        };
        _tray.ForceCreate();

        Host.Start();
    }

    private Microsoft.UI.Xaml.Controls.MenuFlyout BuildMenu()
    {
        var menu = new Microsoft.UI.Xaml.Controls.MenuFlyout();
        var open = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Open" };
        open.Click += (_, _) => TogglePopup();
        var exit = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Exit" };
        exit.Click += (_, _) =>
        {
            _tray?.Dispose();
            Environment.Exit(0);
        };
        menu.Items.Add(open);
        menu.Items.Add(exit);
        return menu;
    }

    private void TogglePopup()
    {
        _window ??= new MeetingsWindow();
        _window.Toggle();
    }
}

public sealed class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _action;

    public RelayCommand(Action action)
    {
        _action = action;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        _action();
    }
}
