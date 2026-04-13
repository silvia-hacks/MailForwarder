using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MailForwarder.Services;
using MailForwarder.ViewModels;
using MailForwarder.Views;

namespace MailForwarder;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIconHost? _trayIconHost;
    private NotificationService? _notificationService;

    public bool IsExitRequested { get; private set; }

    public SettingsService SettingsService { get; private set; } = null!;
    public LogService LogService { get; private set; } = null!;
    public MailReceiveService MailReceiveService { get; private set; } = null!;
    public MailForwardService MailForwardService { get; private set; } = null!;
    public MonitorService MonitorService { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SettingsService = new SettingsService();
        LogService = new LogService();
        MailReceiveService = new MailReceiveService();
        MailForwardService = new MailForwardService();
        var historyService = new TransferHistoryService();
        MonitorService = new MonitorService(SettingsService, LogService, historyService, MailReceiveService, MailForwardService);

        var mainViewModel = new MainViewModel(MonitorService, LogService, SettingsService);
        _mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        _trayIconHost = new TrayIconHost(_mainWindow, mainViewModel);
        _notificationService = new NotificationService(_mainWindow, _trayIconHost, LogService);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }

        LogService.AddInfo("アプリケーションを起動しました。");
        _mainWindow.Hide();

        base.OnFrameworkInitializationCompleted();
    }

    public void RequestShutdown()
    {
        IsExitRequested = true;
        _trayIconHost?.Dispose();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
