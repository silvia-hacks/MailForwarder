using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using MailForwarder.Models;
using MailForwarder.Views;

namespace MailForwarder.Services;

public sealed class NotificationService
{
    private readonly MainWindow _mainWindow;
    private readonly TrayIconHost _trayIconHost;
    private readonly WindowNotificationManager _notificationManager;
    private DateTimeOffset _lastErrorShownAt = DateTimeOffset.MinValue;
    private string _lastErrorMessage = string.Empty;

    public NotificationService(MainWindow mainWindow, TrayIconHost trayIconHost, LogService logService)
    {
        _mainWindow = mainWindow;
        _trayIconHost = trayIconHost;
        _notificationManager = new WindowNotificationManager(_mainWindow)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 3
        };

        logService.EntryAdded += OnEntryAdded;
    }

    private void OnEntryAdded(object? sender, LogEntry entry)
    {
        if (!string.Equals(entry.Level, "エラー", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Avoid flooding the user with repeated notifications for the same error.
        if (_lastErrorMessage == entry.Message &&
            DateTimeOffset.Now - _lastErrorShownAt < TimeSpan.FromMinutes(1))
        {
            return;
        }

        _lastErrorMessage = entry.Message;
        _lastErrorShownAt = DateTimeOffset.Now;

        Dispatcher.UIThread.Post(() =>
        {
            _trayIconHost.ShowMainWindow();
            _notificationManager.Show(new Notification(
                "メール自動転送エラー",
                entry.Message,
                NotificationType.Error,
                TimeSpan.FromSeconds(8)));
        });
    }
}
