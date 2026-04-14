using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MailForwarder.Services;
using MailForwarder.ViewModels;
using MailForwarder.Views;
using System.Threading;
using System.Threading.Tasks;

namespace MailForwarder;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private TrayIconHost? _trayIconHost;
    private NotificationService? _notificationService;
    private EventWaitHandle? _activateMainWindowEvent;
    private CancellationTokenSource? _activateMainWindowCts;
    private Task? _activateMainWindowTask;

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
        StartActivateMainWindowListener();

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
        StopActivateMainWindowListener();
        _trayIconHost?.Dispose();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void StartActivateMainWindowListener()
    {
        _activateMainWindowEvent = Program.CreateActivationEvent();
        _activateMainWindowCts = new CancellationTokenSource();
        var cancellationToken = _activateMainWindowCts.Token;

        _activateMainWindowTask = Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _activateMainWindowEvent.WaitOne();

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                Dispatcher.UIThread.Post(() => _trayIconHost?.ShowMainWindow());
            }
        }, cancellationToken);
    }

    private void StopActivateMainWindowListener()
    {
        _activateMainWindowCts?.Cancel();
        _activateMainWindowEvent?.Set();
        _activateMainWindowEvent?.Dispose();
        _activateMainWindowEvent = null;
        _activateMainWindowCts?.Dispose();
        _activateMainWindowCts = null;
        _activateMainWindowTask = null;
    }
}
