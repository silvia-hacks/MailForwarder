using Avalonia.Controls;
using MailForwarder.ViewModels;
using MailForwarder.Views;
using System.ComponentModel;

namespace MailForwarder.Services;

public sealed class TrayIconHost : IDisposable
{
    private readonly MainWindow _mainWindow;
    private readonly MainViewModel _mainViewModel;
    private readonly TrayIcon _trayIcon;

    public TrayIconHost(MainWindow mainWindow, MainViewModel mainViewModel)
    {
        _mainWindow = mainWindow;
        _mainViewModel = mainViewModel;

        var menu = new NativeMenu();
        var openItem = new NativeMenuItem("開く");
        var startItem = new NativeMenuItem("監視開始");
        var stopItem = new NativeMenuItem("監視停止");
        var separator = new NativeMenuItemSeparator();
        var exitItem = new NativeMenuItem("終了");

        menu.Items.Add(openItem);
        menu.Items.Add(startItem);
        menu.Items.Add(stopItem);
        menu.Items.Add(separator);
        menu.Items.Add(exitItem);

        openItem.Click += (_, _) => ShowMainWindow();
        startItem.Click += (_, _) => _mainViewModel.StartMonitoringCommand.Execute(null);
        stopItem.Click += (_, _) => _mainViewModel.StopMonitoringCommand.Execute(null);
        exitItem.Click += (_, _) => (App.Current as App)?.RequestShutdown();

        _trayIcon = new TrayIcon
        {
            ToolTipText = "メール自動転送",
            IsVisible = true,
            Menu = menu,
            Icon = AppIconService.LoadWindowIcon()
        };

        _trayIcon.Clicked += (_, _) => ShowMainWindow();
        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        UpdateIcon();
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void Dispose()
    {
        _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
        _trayIcon.Dispose();
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsMonitoring))
        {
            UpdateIcon();
        }
    }

    private void UpdateIcon()
    {
        var icon = _mainViewModel.IsMonitoring
            ? AppIconService.LoadMonitoringWindowIcon()
            : AppIconService.LoadWindowIcon();

        _trayIcon.Icon = icon;
        _mainWindow.Icon = icon;
    }
}
