using System.Collections.ObjectModel;
using MailForwarder.Models;
using MailForwarder.Services;

namespace MailForwarder.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly MonitorService _monitorService;
    private readonly LogService _logService;
    private readonly SettingsService _settingsService;
    private string _statusText = "停止中";

    public MainViewModel(
        MonitorService monitorService,
        LogService logService,
        SettingsService settingsService)
    {
        _monitorService = monitorService;
        _logService = logService;
        _settingsService = settingsService;

        Entries = _logService.Entries;
        StartMonitoringCommand = new RelayCommand(_ => StartMonitoring(), _ => !_monitorService.IsRunning);
        StopMonitoringCommand = new RelayCommand(_ => StopMonitoring(), _ => _monitorService.IsRunning);
        OpenSettingsCommand = new RelayCommand(_ => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        RunOnceCommand = new RelayCommand(_ => RunOnce(), _ => !_monitorService.IsCycleRunning);

        _monitorService.StateChanged += (_, _) => RefreshState();
        RefreshState();
    }

    public ObservableCollection<LogEntry> Entries { get; }

    public string ApplicationVersion => $"v{AppVersionService.GetDisplayVersion()}";

    public RelayCommand StartMonitoringCommand { get; }

    public RelayCommand StopMonitoringCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public RelayCommand RunOnceCommand { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ExecutionStatusText => _monitorService.IsCycleRunning ? "実行中..." : string.Empty;

    public bool IsExecuting => _monitorService.IsCycleRunning;

    public bool IsMonitoring => _monitorService.IsRunning;

    public string LastCheckText => _monitorService.LastCheckAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

    public string LastForwardText => _monitorService.LastForwardAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

    public event EventHandler? OpenSettingsRequested;

    private void StartMonitoring()
    {
        _monitorService.Start();
        RefreshState();
    }

    private void StopMonitoring()
    {
        _monitorService.Stop();
        RefreshState();
    }

    private async void RunOnce()
    {
        await _monitorService.ExecuteSingleCycleAsync();
        RefreshState();
    }

    private void RefreshState()
    {
        StatusText = _monitorService.IsRunning ? "監視中" : "停止中";
        OnPropertyChanged(nameof(ExecutionStatusText));
        OnPropertyChanged(nameof(IsExecuting));
        OnPropertyChanged(nameof(IsMonitoring));
        OnPropertyChanged(nameof(LastCheckText));
        OnPropertyChanged(nameof(LastForwardText));
        StartMonitoringCommand.RaiseCanExecuteChanged();
        StopMonitoringCommand.RaiseCanExecuteChanged();
        RunOnceCommand.RaiseCanExecuteChanged();
    }
}
